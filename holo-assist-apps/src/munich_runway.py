from lib import prepare_holo_assist_instance
from lib.holo_assist_types import Color, GeoFixedVertex, WGS84Point

service = prepare_holo_assist_instance()

vertices = [
    GeoFixedVertex(WGS84Point.from_degrees(48.363062, 11.767553, 453), Color(0.0, 1.0, 0.0)),
    GeoFixedVertex(WGS84Point.from_degrees(48.367189, 11.821082, 453), Color(1.0, 0.0, 0.0)),
    GeoFixedVertex(WGS84Point.from_degrees(48.366616, 11.821204, 453), Color(0.0, 0.0, 1.0)),
    GeoFixedVertex(WGS84Point.from_degrees(48.362511, 11.767624, 453), Color(0.5, 0.5, 0.0))
]

indices = [0, 1, 1, 2, 2, 3, 3, 0]
MESH_ID = "EDDM 08 L"

print("Creating mesh...")
service.create_mesh(MESH_ID)
service.add_mesh_vertices(MESH_ID, vertices)
service.add_mesh_indices(MESH_ID, indices)
service.commit_mesh_changes(MESH_ID)
service.activate_mesh(MESH_ID)

input("Press any key to continue...")

print("Mangling indices...")
weird_indices = [0, 1, 1, 3, 3, 2, 2, 0]
service.replace_mesh_indices(MESH_ID, 0, weird_indices)
service.commit_mesh_changes(MESH_ID)

input("Press any key to continue...")

print("Removing mesh...")
service.deactivate_mesh(MESH_ID)
service.delete_mesh(MESH_ID)
