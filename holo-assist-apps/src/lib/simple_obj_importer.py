import re

class ObjLineMesh:
    def __init__(self):
        self.vertices = []
        self.lines = []

def load_obj_line_mesh(path):
    """
        Apparently there isn't a single Python library that is
        able to handle a OBJ file with line elements
    """
    vertex_regex = re.compile("^v ([\\d+-.]+) ([\\d+-.]+) ([\\d+-.]+)$")
    line_regex = re.compile("^l (\\d+) (\\d+)$")

    mesh = ObjLineMesh()

    with open(path, "r", encoding="UTF-8") as mesh_file:
        while True:
            line = mesh_file.readline()

            if not line:
                #End of file
                break

            matcher = vertex_regex.match(line)
            if matcher:
                mesh.vertices.append([
                    float(matcher.group(1)),
                    float(matcher.group(2)),
                    float(matcher.group(3))
                ])
                continue

            matcher = line_regex.match(line)
            if matcher:
                mesh.lines.append([
                    int(matcher.group(1)),
                    int(matcher.group(2))
                ])
                continue

    return mesh
