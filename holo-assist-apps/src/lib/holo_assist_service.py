import socket
import json

from typing import List
from .holo_assist_types import GeoFixedVertex, ColoredVertex, Vector3, Rotation

class HoloAssistService:
    def __init__(self, hololens_ip, hololens_port):
        self.__socket = socket.socket(family=socket.AF_INET, type=socket.SOCK_DGRAM)
        self.__hololens_address = (hololens_ip, hololens_port)

    def __send(self, msg):
        self.__socket.sendto(json.dumps(msg).encode('utf-8'), self.__hololens_address)

    def create_mesh(
        self, mesh_id, interpolate_on_commit = True,
        interpolated_segment_max_length_meters = 30
    ):
        self.__send({
            "type": "CREATE_MESH",
            "id": mesh_id,
            "interpolateOnCommit": interpolate_on_commit,
            "interpolatedSegmentMaxLengthMeters": interpolated_segment_max_length_meters
        })

    def activate_mesh(self, mesh_id):
        self.__send({
            "type": "SET_MESH_ACTIVE",
            "id": mesh_id,
            "active": True
        })

    def deactivate_mesh(self, mesh_id):
        self.__send({
            "type": "SET_MESH_ACTIVE",
            "id": mesh_id,
            "active": False
        })

    def delete_mesh(self, mesh_id):
        self.__send({
            "type": "DELETE_MESH",
            "id": mesh_id
        })

    def commit_mesh_changes(self, mesh_id):
        self.__send({
            "type": "COMMIT_MESH_CHANGES",
            "id": mesh_id
        })

    def add_mesh_vertices(self, mesh_id, vertices):
        self.__send({
            "type": "SET_MESH_VERTICES",
            "id": mesh_id,
            "startIndex": None,
            "vertices": [c.prepare_for_json() for c in vertices]
        })

    def replace_mesh_vertices(self, mesh_id, start_index, vertices: List[GeoFixedVertex]):
        self.__send({
            "type": "SET_MESH_VERTICES",
            "id": mesh_id,
            "startIndex": start_index,
            "vertices": [c.prepare_for_json() for c in vertices]
        })

    def add_mesh_indices(self, mesh_id, indices: List[int]):
        self.__send({
            "type": "SET_MESH_INDICES",
            "id": mesh_id,
            "startIndex": None,
            "indices": indices
        })

    def replace_mesh_indices(self, mesh_id, start_index, indices: List[int]):
        self.__send({
            "type": "SET_MESH_INDICES",
            "id": mesh_id,
            "startIndex": start_index,
            "indices": indices
        })

    def plane_fixed_create_mesh(
        self, mesh_id,
        origin_position = Vector3(0, 0, 0),
        origin_rotation = Rotation(0, 0, 0)
    ):
        self.__send({
            "type": "PF_CREATE_MESH",
            "id": mesh_id,
            "originPositionMeters": origin_position.prepare_for_json(),
            "originRotationRadians": origin_rotation.prepare_for_json()
        })

    def plane_fixed_activate_mesh(self, mesh_id):
        self.__send({
            "type": "PF_SET_MESH_ACTIVE",
            "id": mesh_id,
            "active": True
        })

    def plane_fixed_deactivate_mesh(self, mesh_id):
        self.__send({
            "type": "PF_SET_MESH_ACTIVE",
            "id": mesh_id,
            "active": False
        })

    def plane_fixed_delete_mesh(self, mesh_id):
        self.__send({
            "type": "PF_DELETE_MESH",
            "id": mesh_id
        })

    def plane_fixed_commit_mesh_changes(self, mesh_id):
        self.__send({
            "type": "PF_COMMIT_MESH_CHANGES",
            "id": mesh_id
        })

    def plane_fixed_add_mesh_vertices(self, mesh_id, vertices):
        self.__send({
            "type": "PF_SET_MESH_VERTICES",
            "id": mesh_id,
            "startIndex": None,
            "vertices": [c.prepare_for_json() for c in vertices]
        })

    def plane_fixed_replace_mesh_vertices(
        self, mesh_id, start_index, vertices: List[ColoredVertex]
    ):
        self.__send({
            "type": "PF_SET_MESH_VERTICES",
            "id": mesh_id,
            "startIndex": start_index,
            "vertices": [c.prepare_for_json() for c in vertices]
        })

    def plane_fixed_add_mesh_indices(self, mesh_id, indices: List[int]):
        self.__send({
            "type": "PF_SET_MESH_INDICES",
            "id": mesh_id,
            "startIndex": None,
            "indices": indices
        })

    def plane_fixed_replace_mesh_indices(self, mesh_id, start_index, indices: List[int]):
        self.__send({
            "type": "PF_SET_MESH_INDICES",
            "id": mesh_id,
            "startIndex": start_index,
            "indices": indices
        })

    def plane_fixed_update_mesh_origin(
        self, mesh_id,
        origin_position: Vector3 = None,
        origin_rotation: Rotation = None
    ):
        msg = {
            "type": "PF_UPDATE_MESH_ORIGIN",
            "id": mesh_id,
        }

        if origin_position is not None:
            msg["originPositionMeters"] = origin_position.prepare_for_json()

        if origin_rotation is not None:
            msg["originRotationRadians"] = origin_rotation.prepare_for_json()

        self.__send(msg)
