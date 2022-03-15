import csv
import os
import time
from typing import List

from lib.holo_assist_types import GeoFixedVertex, WGS84Point, Color, Rotation, Vector3
from lib import prepare_holo_assist_instance

csv.register_dialect("my", skipinitialspace=True, strict=True)

def permissive_float_conversion(n: str):
    # Technically this is wrong, as it will mess
    # up a number like "1,234.56", but it is good
    # enough given the current pipeline
    return float(n.replace(",", "."))

class TunnelCommand:
    def __init__(self, csv_line: List[str]):
        self.tunnel_id = int(csv_line[0])
        self.slice_id = int(csv_line[1])

        FEET_TO_METERS = 0.3048
        self.position_wgs = WGS84Point.from_degrees(
            permissive_float_conversion(csv_line[2]), permissive_float_conversion(csv_line[3]),
            permissive_float_conversion(csv_line[4]) * FEET_TO_METERS
        )

        self.roll_deg = permissive_float_conversion(csv_line[5])
        self.pitch_deg = permissive_float_conversion(csv_line[6])
        self.heading_deg = permissive_float_conversion(csv_line[7])

        self.rectangle_width_m = permissive_float_conversion(csv_line[8])
        self.rectangle_height_m = permissive_float_conversion(csv_line[9])

        self.setting = int(csv_line[10])

        self.__color_r = permissive_float_conversion(csv_line[2])
        self.__color_g = permissive_float_conversion(csv_line[3])
        self.__color_b = permissive_float_conversion(csv_line[4])
        self.__line_width = permissive_float_conversion(csv_line[5])

    def is_end_of_data(self):
        return self.setting == 90

    def is_delete_stored_tunnel_data(self):
        return self.setting == 99

    def is_change_tunnel_color(self):
        return self.setting == 80

    def get_color_and_line_width(self):
        assert self.is_change_tunnel_color()
        c = Color(self.__color_r, self.__color_b, self.__color_g)
        return (c, self.__line_width)

    def should_draw_rectangle(self):
        return self.setting in [0, 2]

    def should_draw_line_to_previous_rectangle(self):
        return self.setting in [0, 1]


def basic_rectangle(width, height):
    vertices = [
        Vector3(-width / 2, 0, +height/2),
        Vector3(+width / 2, 0, +height/2),
        Vector3(+width / 2, 0, -height/2),
        Vector3(-width / 2, 0, -height/2),
    ]

    indices = [0, 1, 1, 2, 2, 3, 3, 0]

    return (vertices, indices)

tunnel_commands: List[TunnelCommand] = []
with open(os.path.join("data", "test-tunnel-for-tunnel-api.csv"), encoding="UTF-8") as file:
    reader = csv.reader(file, dialect="my")
    for row in reader:
        if len(row) == 0 or row[0].startswith("#"):
            continue
        tunnel_commands.append(TunnelCommand(row))

service = prepare_holo_assist_instance()
current_color_for_tunnel = {}
current_slice_for_tunnel = {}

DELAY_SECONDS = 0.3

for cmd in tunnel_commands:
    tid = f"TG_TUNNEL_{cmd.tunnel_id}"

    if cmd.is_delete_stored_tunnel_data():
        service.delete_mesh(tid)
        time.sleep(DELAY_SECONDS)
        continue

    if cmd.is_end_of_data():
        service.commit_mesh_changes(tid)
        time.sleep(DELAY_SECONDS)
        continue

    if cmd.is_change_tunnel_color():
        current_color_for_tunnel[cmd.tunnel_id] = cmd.get_color_and_line_width()
        time.sleep(DELAY_SECONDS)
        continue

    assert cmd.should_draw_rectangle() or cmd.should_draw_line_to_previous_rectangle()

    (vertices, indices) = basic_rectangle(cmd.rectangle_width_m, cmd.rectangle_height_m)
    assert len(vertices) == 4
    assert len(indices) == 8

    wgs = cmd.position_wgs
    col = current_color_for_tunnel[cmd.tunnel_id][0] #Line width is ignored, as it is not supported by HoloAssist
    rot = Rotation.from_degrees(cmd.pitch_deg, cmd.roll_deg, -cmd.heading_deg)
    geo_vertices = [GeoFixedVertex(wgs, col, v, rot) for v in vertices]

    current_index = (cmd.slice_id - 1) * 4
    indices = [i + current_index for i in indices]

    if (cmd.slice_id - 1) == 0:
        # The first slice cannot "link back" to the previous tunnel rectangle,
        # but the four slots for the indices must still be taken
        indices.extend([0, 0, 0, 0, 0, 0, 0, 0])
    else:
        indices.extend([
            current_index - 4 + 0, current_index + 0,
            current_index - 4 + 1, current_index + 1,
            current_index - 4 + 2, current_index + 2,
            current_index - 4 + 3, current_index + 3,
        ])

    if cmd.tunnel_id not in current_slice_for_tunnel:
        current_slice_for_tunnel[cmd.tunnel_id] = 0
        service.create_mesh(tid)
        time.sleep(DELAY_SECONDS)
        service.activate_mesh(tid)
        time.sleep(DELAY_SECONDS)

    if not cmd.should_draw_rectangle():
        indices[0:8] = [0, 0, 0, 0, 0, 0, 0, 0]

    if not cmd.should_draw_line_to_previous_rectangle():
        indices[8:16] = [0, 0, 0, 0, 0, 0, 0, 0]

    if current_slice_for_tunnel[cmd.tunnel_id] < cmd.slice_id:
        assert current_slice_for_tunnel[cmd.tunnel_id] + 1 == cmd.slice_id
        current_slice_for_tunnel[cmd.tunnel_id] = cmd.slice_id
        service.add_mesh_vertices(tid, geo_vertices)
        time.sleep(DELAY_SECONDS)
        service.add_mesh_indices(tid, indices)
        time.sleep(DELAY_SECONDS)
    else:
        service.replace_mesh_vertices(tid, current_index, geo_vertices)
        time.sleep(DELAY_SECONDS)
        service.replace_mesh_indices(tid, cmd.slice_id * 16, indices)
        time.sleep(DELAY_SECONDS)
