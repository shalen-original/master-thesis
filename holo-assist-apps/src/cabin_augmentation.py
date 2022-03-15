from time import sleep
from lib import prepare_holo_assist_instance
from lib.holo_assist_types import Vector3, Color, ColoredVertex, Rotation

service = prepare_holo_assist_instance()

vertices = [
    Vector3(0.494726, 0.368722, 1.003500),
    Vector3(0.494726, 0.253980, 0.955498),
    Vector3(0.619104, 0.368722, 1.003500),
    Vector3(0.619104, 0.253980, 0.955498),
]

colors = [
    Color(0.0, 1.0, 0.0),
    Color(1.0, 0.0, 0.0),
    Color(0.0, 0.0, 1.0),
    Color(0.5, 0.5, 0.0)
]

colored_vertices = [ColoredVertex(point, color) for (point, color) in zip(vertices, colors)]
indices = [2, 0, 0, 1, 1, 3, 3, 2]
MESH_ID = "GEAR_LEVER"

print("Creating mesh...")
service = prepare_holo_assist_instance()
service.plane_fixed_create_mesh(MESH_ID)
service.plane_fixed_add_mesh_vertices(MESH_ID, colored_vertices)
service.plane_fixed_add_mesh_indices(MESH_ID, indices)
service.plane_fixed_commit_mesh_changes(MESH_ID)
service.plane_fixed_activate_mesh(MESH_ID)

input("Press any key to continue...")

print("Mangling indices...")
weird_indices = [0, 1, 1, 3, 3, 2, 2, 0]
service.plane_fixed_replace_mesh_indices(MESH_ID, 0, weird_indices)
service.plane_fixed_commit_mesh_changes(MESH_ID)

input("Press any key to continue...")

print("Rotating mesh...")
for i in range (0, 30):
    service.plane_fixed_update_mesh_origin(
        MESH_ID, origin_rotation=Rotation(0, 0, i)
    )
    sleep(0.1)
for i in range (30, 0, -1):
    service.plane_fixed_update_mesh_origin(
        MESH_ID, origin_rotation=Rotation(0, 0, i)
    )
    sleep(0.1)

input("Press any key to continue...")

print("Translating mesh...")
for i in range (0, 10):
    service.plane_fixed_update_mesh_origin(
        MESH_ID, origin_position=Vector3(0, 0, -i / 10.0)
    )
    sleep(0.5)

input("Press any key to continue...")

print("Removing mesh...")
service.deactivate_mesh(MESH_ID)
service.delete_mesh(MESH_ID)
