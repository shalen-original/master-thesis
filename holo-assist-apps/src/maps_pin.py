import os
from time import sleep

from lib import simple_obj_importer
from lib import prepare_holo_assist_instance, convert_obj_to_geo_fixed_mesh
from lib.holo_assist_types import Color, Rotation, WGS84Point

MESH_ID = "MAP PIN"
PIN_COLOR = Color(0.0, 0.8, 0.0)
PIN_POSITION = WGS84Point.from_degrees(48.363062, 11.767553, 453)
PIN_OTHER_POSITION = WGS84Point.from_degrees(48.26606999695851, 11.668464334433848, 1000)
PIN_ROTATION = Rotation.from_degrees(0, 0, 0)

map_pin_obj = simple_obj_importer.load_obj_line_mesh(os.path.join("data", "map-pin.obj"))
(vertices, indices) = convert_obj_to_geo_fixed_mesh(map_pin_obj, PIN_COLOR, PIN_POSITION, PIN_ROTATION)

service = prepare_holo_assist_instance()

print("Creating mesh...")
service.create_mesh(MESH_ID, interpolated_segment_max_length_meters=3)
service.add_mesh_vertices(MESH_ID, vertices)
service.add_mesh_indices(MESH_ID, indices)
service.commit_mesh_changes(MESH_ID)
service.activate_mesh(MESH_ID)

input("Press a key to continue")
print("Rotating mesh...")
for i in range (0, 361, 20):
    new_rotation = Rotation.from_degrees(0, 0, i)
    (vertices, _) = convert_obj_to_geo_fixed_mesh(
        map_pin_obj, PIN_COLOR, PIN_POSITION, new_rotation
    )
    service.replace_mesh_vertices(MESH_ID, 0, vertices)
    service.commit_mesh_changes(MESH_ID)
    sleep(0.2)

input("Press a key to continue")
print("Moving point to another place...")
(vertices, _) = convert_obj_to_geo_fixed_mesh(
    map_pin_obj, PIN_COLOR, PIN_OTHER_POSITION, PIN_ROTATION
)
service.replace_mesh_vertices(MESH_ID, 0, vertices)
service.commit_mesh_changes(MESH_ID)

input("Press any key to continue...")
print("Removing mesh...")
service.deactivate_mesh(MESH_ID)
service.delete_mesh(MESH_ID)
