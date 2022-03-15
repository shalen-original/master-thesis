import sys
import itertools

from .holo_assist_types import Color, WGS84Point, Rotation, GeoFixedVertex, Vector3
from .holo_assist_service import HoloAssistService
from .simple_obj_importer import ObjLineMesh

def prepare_holo_assist_instance():
    send_to_hololens = True

    if len(sys.argv) > 1 and sys.argv[1] == "--unity":
        send_to_hololens = False

    if send_to_hololens:
        return HoloAssistService("192.168.0.200", 53941)

    return HoloAssistService("127.0.0.1", 53941)

def convert_obj_to_geo_fixed_mesh(
    mesh: ObjLineMesh, mesh_color: Color,
    mesh_geo_position: WGS84Point,
    mesh_local_rotation: Rotation):

    # OBJ files have no intrinsic understanding of "left-handed" vs
    # "right-handed" coordinate systems, they just transfer raw coordinates.
    # When exporting an OBJ file from Blender, its coordinates will be encoded
    # in a right-handed coordinate systems, because Blender main coordinate system
    # is right-handed. However, Unity (and HoloAssist) have left-handed coordinate systems.
    # When loading the OBJ, therefore, an appropriate correction must be applied, otherwise
    # the mesh will look "chirally opposite" when loaded in Unity, and this has been the
    # source of many headaches.
    # The "-" sign in front of point[0] takes care of this conversion.

    vertices = [
        GeoFixedVertex(
            mesh_geo_position, mesh_color,
            Vector3(-point[0], point[1], point[2]), mesh_local_rotation
        ) for point in mesh.vertices
    ]

    flattened_indices = list(itertools.chain.from_iterable(mesh.lines))
    indices_in_base_zero = [i - 1 for i in flattened_indices]

    return (vertices, indices_in_base_zero)
