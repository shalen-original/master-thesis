import os

from lib import simple_obj_importer
from lib import prepare_holo_assist_instance, convert_obj_to_geo_fixed_mesh
from lib.holo_assist_types import Color, Rotation, WGS84Point

MESH_ID = "INNSBRUCK_TERRAIN_BASIC"
TERRAIN_COLOR = Color(0.5, 0.5, 0.0)
TERRAIN_POSITION = WGS84Point.from_degrees(47.2651649542, 11.3186282186, 580 + 20)
TERRAIN_ROTATION = Rotation.from_degrees(0, 0, 0)

terrain_obj = simple_obj_importer.load_obj_line_mesh(os.path.join("data", "3d-terrain.obj"))
(vertices, indices) = convert_obj_to_geo_fixed_mesh(
    terrain_obj, TERRAIN_COLOR, TERRAIN_POSITION, TERRAIN_ROTATION
)

vertices_A = vertices[:len(vertices) // 2]
vertices_B = vertices[len(vertices) // 2:]

service = prepare_holo_assist_instance()

service.create_mesh(MESH_ID)
service.add_mesh_vertices(MESH_ID, vertices_A)
service.add_mesh_vertices(MESH_ID, vertices_B)
service.add_mesh_indices(MESH_ID, indices)
service.commit_mesh_changes(MESH_ID)
service.activate_mesh(MESH_ID)

input("")
service.delete_mesh(MESH_ID)
