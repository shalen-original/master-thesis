import csv
import math
import os

import numpy as np
import pyproj
import scipy, scipy.interpolate

from lib import prepare_holo_assist_instance
from lib.holo_assist_types import Rotation, Vector3, WGS84Point, Color, GeoFixedVertex

csv.register_dialect("my", skipinitialspace=True, strict=True)

def normalize(vector):
    #https://stackoverflow.com/q/21030391
    norm=np.linalg.norm(vector)
    if norm==0:
        norm=np.finfo(vector.dtype).eps
    return vector/norm

def read_csv():
    pts = []
    with open(os.path.join("data", "innsbruck-RNP-Y-RWY-08.csv"), encoding="UTF-8") as file:
        reader = csv.reader(file, dialect="my")
        reader.__next__() #Skip first line (headings)
        for row in reader:
            pts.append(
                (float(row[1]), float(row[2]), float(row[3]))
            )
    return pts

def create_rectangle(position: WGS84Point, color: Color,
                    width: float, height: float,
                    normal: np.ndarray, tangent = np.array([0, 0, -1])):
    DEFAULT_NORMAL = np.array([0, 1, 0])
    DEFAULT_TANGENT = np.array([0, 0, -1])

    # Describes the vertices of a rectangle which has
    # normal DEFAULT_NORMAL and tangent DEFAULT_TANGENT
    # The origin of the coordinate system is the origin
    # of the ENU system centered at `position`
    vertices = [
        Vector3(-width / 2, 0, +height/2),
        Vector3(+width / 2, 0, +height/2),
        Vector3(+width / 2, 0, -height/2),
        Vector3(-width / 2, 0, -height/2),
    ]

    indices = [0, 1, 1, 2, 2, 3, 3, 0]

    rot_default_to_desired, error = scipy.spatial.transform.Rotation.align_vectors(
        [DEFAULT_NORMAL, DEFAULT_TANGENT],
        [normal, tangent]
    )

    assert error < 0.5

    rotation = Rotation(*rot_default_to_desired.as_euler("XYZ"))
    geo_vertices = [GeoFixedVertex(position, color, v, rotation) for v in vertices]

    return (geo_vertices, indices)

def create_tunnel_mesh(csv_points):
    points = []
    indices = []
    current_index = 0

    color = Color(0.3, 0.3, 0.0)

    for point in csv_points:
        pos = WGS84Point.from_degrees(*point)
        (geo_pts, ics) = create_rectangle(pos, color, 200, 200, [1.0, 0.0, 0.0])
        points.extend(geo_pts)
        indices.extend([i + current_index for i in ics])

        # The first square of the tunnel does not have a "previous"
        # square to connect to
        if current_index != 0:
            indices.extend([
                current_index - 4 + 0, current_index + 0,
                current_index - 4 + 1, current_index + 1,
                current_index - 4 + 2, current_index + 2,
                current_index - 4 + 3, current_index + 3,
            ])

        current_index += len(geo_pts)

    return (points, indices)

def compute_spline(csv_points):
    wgs84_crs = pyproj.CRS.from_epsg(4979)
    ecef_crs = pyproj.CRS.from_epsg(4978)
    transformer_wgs2ecef = pyproj.Transformer.from_crs(wgs84_crs, ecef_crs)

    xs, ys, zs = [], [], []

    csv_points = [np.array(list(transformer_wgs2ecef.transform(*p))) for p in csv_points]

    for i in range(0, len(csv_points)):
        curr = csv_points[i]

        if i != 0:
            prev = csv_points[i - 1]
            p = prev + 0.7 * (curr - prev)
            xs.append(p[0])
            ys.append(p[1])
            zs.append(p[2])

        if i != len(csv_points) - 1:
            succ = csv_points[i + 1]
            p = succ + 0.7 * (curr - succ)
            xs.append(p[0])
            ys.append(p[1])
            zs.append(p[2])

        if i == 0 or i == len(csv_points) - 1:
            xs.append(csv_points[i][0])
            ys.append(csv_points[i][1])
            zs.append(csv_points[i][2])

    w = np.ones(len(xs)) * 0.4
    w[0] = 1
    w[-1] = 1

    tck, u = scipy.interpolate.splprep([xs, ys, zs], w, s=2000)
    u = np.linspace(0, 1, 250 // 4)
    spline_points = scipy.interpolate.splev(u, tck)
    spline_points_der_1 = scipy.interpolate.splev(u, tck, der=1)

    points = []
    for i in range(0, len(spline_points[0])):
        p_ecef = [spline_points[0][i], spline_points[1][i], spline_points[2][i]]
        d1 = np.array([spline_points_der_1[0][i], spline_points_der_1[1][i], spline_points_der_1[2][i]])
        points.append((p_ecef, d1))

    return points

def ecef_to_enu(p_ecef, enu_origin: WGS84Point):
    phi = enu_origin.latitude_rad
    llambda = enu_origin.longitude_rad

    rotation = np.array([
        [ -math.sin(llambda),  math.cos(llambda), 0, 0 ],
        [ -math.cos(llambda) * math.sin(phi),  -math.sin(llambda) * math.sin(phi), math.cos(phi), 0 ],
        [ math.cos(llambda) * math.cos(phi),  math.sin(llambda) * math.cos(phi), math.sin(phi), 0 ],
        [ 0, 0, 0, 1 ]
    ])

    wgs84_crs = pyproj.CRS.from_epsg(4979)
    ecef_crs = pyproj.CRS.from_epsg(4978)
    transformer_wgs2ecef = pyproj.Transformer.from_crs(wgs84_crs, ecef_crs)
    enu_origin_wgs_deg = [np.rad2deg(enu_origin.latitude_rad), np.rad2deg(enu_origin.longitude_rad), enu_origin.altitude_meters]
    enu_origin_ecef = np.array([*transformer_wgs2ecef.transform(*enu_origin_wgs_deg), 0])

    p_ecef = np.array([*p_ecef, 1])
    enu = np.matmul(rotation, p_ecef - enu_origin_ecef)

    return np.array([enu[0], enu[2], enu[1]])

def create_spline_tunnel_mesh(csv_points):
    color = Color(0, 0.3, 0.5)

    wgs84_crs = pyproj.CRS.from_epsg(4979)
    ecef_crs = pyproj.CRS.from_epsg(4978)
    transformer_ecef2wgs = pyproj.Transformer.from_crs(ecef_crs, wgs84_crs)

    spline_points = compute_spline(csv_points)

    vertices = []
    indices = []
    current_index = 0

    for i in range(0, len(spline_points)):
        (point_ecef, d1) = spline_points[i]
        (lat_deg, lon_deg, alt_m) = transformer_ecef2wgs.transform(*point_ecef)
        point_wgs = WGS84Point.from_degrees(lat_deg, lon_deg, alt_m)
        
        spline_tangent_enu = normalize(ecef_to_enu(point_ecef + d1, point_wgs))

        (rect_points, rect_indices) = create_rectangle(point_wgs, color, 200, 200, spline_tangent_enu)

        vertices.extend(rect_points)
        indices.extend([i + current_index for i in rect_indices])

        # The first square of the tunnel does not have a "previous"
        # square to connect to
        if current_index != 0:
            indices.extend([
                current_index - 4 + 0, current_index + 0,
                current_index - 4 + 1, current_index + 1,
                current_index - 4 + 2, current_index + 2,
                current_index - 4 + 3, current_index + 3,
            ])

        current_index += len(rect_points)

    return (vertices, indices)

def chunks(lst, n):
    """Yield successive n-sized chunks from lst."""
    #https://stackoverflow.com/a/312464
    for i in range(0, len(lst), n):
        yield lst[i:i + n]

def main():
    csv_points = read_csv()
    (vertices_line, indices_line) = create_tunnel_mesh(csv_points)
    (vertices_spline, indices_spline) = create_spline_tunnel_mesh(csv_points)

    service = prepare_holo_assist_instance()

    mesh_id = "RPN Y RWY 08 - Line"
    service.create_mesh(mesh_id)

    for vs in chunks(vertices_line, 100):
        service.add_mesh_vertices(mesh_id, vs)

    for inds in chunks(indices_line, 500):
        service.add_mesh_indices(mesh_id, inds)

    #service.commit_mesh_changes(mesh_id)
    #service.activate_mesh(mesh_id)

    mesh_id_spline = "RPN Y RWY 08 - Spline"
    service.create_mesh(mesh_id_spline)

    for vs in chunks(vertices_spline, 100):
        service.add_mesh_vertices(mesh_id_spline, vs)

    for inds in chunks(indices_spline, 500):
        service.add_mesh_indices(mesh_id_spline, inds)

    service.commit_mesh_changes(mesh_id_spline)
    service.activate_mesh(mesh_id_spline)

    input()
    service.delete_mesh(mesh_id)
    service.delete_mesh(mesh_id_spline)

main()
