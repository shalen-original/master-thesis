import math

class WGS84Point:
    def __init__(self, latitude_rad, longitude_rad, altitude_meters):
        self.latitude_rad = latitude_rad
        self.longitude_rad = longitude_rad
        self.altitude_meters = altitude_meters

    @staticmethod
    def from_degrees(latitude_degrees, longitude_degrees, altitude_meters):
        deg_to_rad = math.pi / 180

        return WGS84Point(
            latitude_degrees * deg_to_rad,
            longitude_degrees * deg_to_rad,
            altitude_meters
        )

    def prepare_for_json(self):
        return {
            "latitudeRadians": self.latitude_rad,
            "longitudeRadians": self.longitude_rad,
            "altitudeMeters": self.altitude_meters
        }

    def raw_to_vector3(self):
        return Vector3(self.latitude_rad, self.longitude_rad, self.altitude_meters)

    def __repr__(self):
        return f"WGS84Point({self.latitude_rad}, {self.longitude_rad}, {self.altitude_meters})"

class Vector3:
    def __init__(self, x, y, z):
        self.x = x
        self.y = y
        self.z = z

    def prepare_for_json(self):
        return [self.x, self.y, self.z]

    def __repr__(self):
        return f"Vector3({self.x}, {self.y}, {self.z})"

class Color:
    def __init__(self, red, green, blue):
        assert 0 <= red <= 1.0
        assert 0 <= green <= 1.0
        assert 0 <= blue <= 1.0

        self.red = red
        self.green = green
        self.blue = blue

    def prepare_for_json(self):
        return [self.red, self.green, self.blue, 1.0]

    def __repr__(self):
        return f"Color({self.red}, {self.green}, {self.blue})"

class Rotation:
    def __init__(self, localx_radians, localy_radians, localz_radians):
        self.localx_radians = localx_radians
        self.localy_radians = localy_radians
        self.localz_radians = localz_radians

    @staticmethod
    def from_degrees(localx_degrees, localy_degrees, localz_degrees):
        deg_to_rad = math.pi / 180

        return Rotation(
            localx_degrees * deg_to_rad,
            localy_degrees * deg_to_rad,
            localz_degrees * deg_to_rad
        )

    def prepare_for_json(self):
        return [self.localx_radians, self.localy_radians, self.localz_radians]

    def __repr__(self):
        return f"Rotation({self.localx_radians}, {self.localy_radians}, {self.localz_radians})"

class GeoFixedVertex:
    def __init__(self, origin_wgs: WGS84Point, color: Color,
                local_position: Vector3 = Vector3(0, 0, 0),
                local_rotation: Rotation = Rotation(0, 0, 0)):
        self.origin_wgs = origin_wgs
        self.color = color
        self.local_position = local_position
        self.local_rotation = local_rotation

    def prepare_for_json(self):
        return {
            "originWgs": self.origin_wgs.prepare_for_json(),
            "color": self.color.prepare_for_json(),
            "localPositionMeters": self.local_position.prepare_for_json(),
            "localRotationRadians": self.local_rotation.prepare_for_json()
        }

    def __repr__(self):
        return f"Vertex({self.origin_wgs}, {self.color}, " + \
            f"{self.local_position}, {self.local_rotation})"

class ColoredVertex:
    def __init__(self, position: Vector3, color: Color):
        self.position = position
        self.color = color

    def prepare_for_json(self):
        return {
            "position": self.position.prepare_for_json(),
            "color": self.color.prepare_for_json()
        }

    def __repr__(self):
        return f"ColoredVertex({self.position}, {self.color})"