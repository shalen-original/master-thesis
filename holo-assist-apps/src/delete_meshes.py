import time
from lib import prepare_holo_assist_instance

service = prepare_holo_assist_instance()

MESH_IDS = [
    "RPN Y RWY 08 - Line",
    "RPN Y RWY 08 - Spline",
    "LOWI RWY 08",
    "INNSBRUCK_TERRAIN",
    "INNSBRUCK_TERRAIN_BASIC",
    "MAP PIN",
    "EDDM 08 L",
    "TG_TUNNEL_1",
    "TG_TUNNEL_2",
    "TG_TUNNEL_3",
    "TG_TUNNEL_4",
    "TG_TUNNEL_5",
    "TG_TUNNEL_6",
    "TG_TUNNEL_7",
    "TG_TUNNEL_8",
    "TG_TUNNEL_9",
    "TG_TUNNEL_10",
    "TG_TUNNEL_11",
    "TG_TUNNEL_12",
    "TG_TUNNEL_13",
    "TG_TUNNEL_14",
]

for mesh_id in MESH_IDS:
    service.delete_mesh(mesh_id)
    time.sleep(0.3)