import math
import os
import socket
import struct
import pyproj
import numpy as np

from lib import simple_obj_importer
from lib import prepare_holo_assist_instance, convert_obj_to_geo_fixed_mesh
from lib.holo_assist_types import Color, Rotation, WGS84Point

def process_buttons_socket(socket, current_max_distance):
    while True:
        try:
            msg_byts_button = socket.recv(9*8)
            [_, _, _, _, _, _, distance_one, distance_two, distance_three] = struct.unpack('<9d', msg_byts_button)

            if distance_one == 1:
                current_max_distance = 2.5
            elif distance_two == 1:
                current_max_distance = 5
            elif distance_three == 1:
                current_max_distance = 10
        except BlockingIOError:
            break

    return current_max_distance

def to_ecef(enu_point, enu_origin_wgs: WGS84Point):
    phi = enu_origin_wgs.latitude_rad
    llambda = enu_origin_wgs.longitude_rad

    rot = np.array([
        [-math.sin(llambda), -math.sin(phi) * math.cos(llambda), math.cos(phi) * math.cos(llambda), 0],
        [math.cos(llambda), -math.sin(phi) * math.sin(llambda), math.cos(phi) * math.sin(llambda), 0],
        [0, math.cos(phi), math.sin(phi), 0],
        [0, 0, 0, 1]
    ])

    ecef_vector4 = np.dot(rot, enu_point)
    ecef = np.array([ecef_vector4[0], ecef_vector4[1], ecef_vector4[2]])

    wgs84_crs = pyproj.CRS.from_epsg(4979)
    ecef_crs = pyproj.CRS.from_epsg(4978)
    transformer_wgs2ecef = pyproj.Transformer.from_crs(wgs84_crs, ecef_crs)

    rad2deg = 180 / 3.1415
    (a, b, c) = transformer_wgs2ecef.transform(
        enu_origin_wgs.latitude_rad * rad2deg,
        enu_origin_wgs.longitude_rad * rad2deg,
        enu_origin_wgs.altitude_meters
    )
    enu_origin_ecef = np.array([a, b, c])

    return ecef + enu_origin_ecef

def main():
    MESH_ID = "INNSBRUCK_TERRAIN"
    TERRAIN_COLOR = Color(0.4, 0.0, 0.0)
    TERRAIN_POSITION = WGS84Point.from_degrees(47.2651649542, 11.3186282186, 580 + 20)
    TERRAIN_ROTATION = Rotation.from_degrees(0, 0, 0)
    NUMBER_OF_PACKETS_TO_DISCARD = 10
    DEFAULT_MAXIMUM_DISTANCE_TO_SHOW_NAUTICAL_MILES = 2.5
    BUTTONS_MULTICAST_GROUP = '231.8.8.8'
    BUTTONS_MULTICAST_PORT = 20203

    terrain_obj = simple_obj_importer.load_obj_line_mesh(os.path.join("data", "3d-terrain.obj"))
    (vertices, indices) = convert_obj_to_geo_fixed_mesh(
        terrain_obj, TERRAIN_COLOR, TERRAIN_POSITION, TERRAIN_ROTATION
    )

    udp_socket_sim_position = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    udp_socket_sim_position.bind(("192.168.0.202", 53941))
    udp_socket_sim_position.settimeout(1)

    udp_socket_buttons = socket.socket(socket.AF_INET, socket.SOCK_DGRAM, socket.IPPROTO_UDP)
    udp_socket_buttons.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    udp_socket_buttons.bind(('', BUTTONS_MULTICAST_PORT))
    mreq = struct.pack("4sl", socket.inet_aton(BUTTONS_MULTICAST_GROUP), socket.INADDR_ANY)
    udp_socket_buttons.setsockopt(socket.IPPROTO_IP, socket.IP_ADD_MEMBERSHIP, mreq)
    udp_socket_buttons.setblocking(0)

    service = prepare_holo_assist_instance()
    service.create_mesh(MESH_ID)
    service.add_mesh_vertices(MESH_ID, vertices[:len(vertices) // 2])
    service.add_mesh_vertices(MESH_ID, vertices[len(vertices) // 2:])
    service.add_mesh_indices(MESH_ID, indices)
    service.commit_mesh_changes(MESH_ID)
    service.activate_mesh(MESH_ID)

    wgs84_crs = pyproj.CRS.from_epsg(4979)
    ecef_crs = pyproj.CRS.from_epsg(4978)
    transformer_wgs2ecef = pyproj.Transformer.from_crs(wgs84_crs, ecef_crs)

    rad2deg = 180 / 3.1415
    nauticalmiles2meters = 1852

    vertices_in_ecef = []
    for v in vertices:
        v_as_vector4 = np.array([v.local_position.x, v.local_position.y, v.local_position.z, 1])
        vertices_in_ecef.append(to_ecef(v_as_vector4, TERRAIN_POSITION))
    assert(len(vertices_in_ecef) == len(vertices))

    filtered_indices = list(np.zeros(len(indices)))

    counter = NUMBER_OF_PACKETS_TO_DISCARD
    current_max_distance = DEFAULT_MAXIMUM_DISTANCE_TO_SHOW_NAUTICAL_MILES

    sim_position_update_packet_size = 97

    while True:
        try:
            (msg_bytes, _) = udp_socket_sim_position.recvfrom(sim_position_update_packet_size)
            [_, lat_rad, lon_rad, alt_m] = struct.unpack('<c3d', msg_bytes[0:25])

            assert(msg_bytes[0] == 0)
            assert(lat_rad is not math.nan)

            current_max_distance = process_buttons_socket(udp_socket_buttons, current_max_distance)

            # Only process one every NUMBER_OF_PACKETS_TO_DISCARD 
            # simulator position update UDP packets
            counter += 1
            if counter < NUMBER_OF_PACKETS_TO_DISCARD:
                continue
            else:
                counter = 0

            (p_x, p_y, p_z) = transformer_wgs2ecef.transform(lat_rad * rad2deg, lon_rad * rad2deg, alt_m)
            plane_ecef = np.array([p_x, p_y, p_z])

            for i in range(0, len(vertices_in_ecef)):
                # This only works in this case (no local rotation), as
                # local_rotation is not accounted for
                distance = np.linalg.norm(vertices_in_ecef[i] - plane_ecef)

                for j in range(0, len(indices), 2):
                    if indices[j] == i or indices[j + 1] == i:
                        if distance < current_max_distance * nauticalmiles2meters:
                            filtered_indices[j] = indices[j]
                            filtered_indices[j + 1] = indices[j + 1]
                        else:
                            filtered_indices[j] = 0
                            filtered_indices[j + 1] = 0

            service.replace_mesh_indices(MESH_ID, 0, filtered_indices)
            service.commit_mesh_changes(MESH_ID)

        except socket.timeout:
            continue

main()