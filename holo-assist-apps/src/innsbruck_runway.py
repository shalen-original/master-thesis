import os
import geojson

from lib import prepare_holo_assist_instance
from lib.holo_assist_types import WGS84Point, Color, GeoFixedVertex

def load_runway_coordinates():
    with open(os.path.join("data", "innsbruck-RWY-08.geojson"), "r", encoding="UTF-8") as file:
        runway_geojson = geojson.loads(file.read())

        assert runway_geojson.is_valid

        ans = []
        for feature in runway_geojson["features"]:
            assert feature["type"] == "Feature"
            assert feature["geometry"]["type"] == "Point"
            altitude = feature["properties"]["Altitude [m]"]
            coords = feature["geometry"]["coordinates"]
            ans.append(WGS84Point.from_degrees(coords[1], coords[0], altitude))

        return ans

runway_color = Color(0, 0.2, 0)
runway_points = load_runway_coordinates()
runway_vertices = [GeoFixedVertex(p, runway_color) for p in runway_points]

MESH_ID = "LOWI RWY 08"
runway_indices = [0, 1, 1, 2, 2, 3, 3, 0]

service = prepare_holo_assist_instance()

service.create_mesh(MESH_ID)
service.add_mesh_vertices(MESH_ID, runway_vertices)
service.add_mesh_indices(MESH_ID, runway_indices)
service.commit_mesh_changes(MESH_ID)
service.activate_mesh(MESH_ID)

input()
service.delete_mesh(MESH_ID)
