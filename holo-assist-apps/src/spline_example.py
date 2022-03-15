import numpy as np
from scipy.interpolate import splprep, splev
import matplotlib.pyplot as plt

x, y, z = [0, 1, 2, 3, 4, 5], [1, 5, 3, 7, 1, 2], [2, 5, 4, 1, 5, 2]

tck, u = splprep([x, y, z], s=0)
my_u = np.linspace(0, 1, 100)
new_points = splev(my_u, tck)
new_points_der_1 = splev(my_u, tck, der=1)
new_points_der_2 = splev(my_u, tck, der=2)

fig = plt.figure()
ax = fig.add_subplot(projection='3d')
ax.scatter(x, y, z, 'ro')
ax.plot(new_points[0], new_points[1], new_points[2], 'r-')

for i in range(0, len(new_points[0]), 10):
    d1 = np.array([new_points_der_1[0][i], new_points_der_1[1][i], new_points_der_1[2][i]])
    d2 = np.array([new_points_der_2[0][i], new_points_der_2[1][i], new_points_der_2[2][i]])

    # https://stackoverflow.com/a/62546217
    m1 = 1.0 / (np.linalg.norm(d1) ** 2)
    m2 = m1 * m1 * np.dot(d1, d2)
    k = m1 * d2 - m2 * d1
    radius_of_curvature = k / (np.linalg.norm(k) ** 2)

    d1 = d1 / (np.linalg.norm(d1))
    radius_of_curvature = radius_of_curvature / (np.linalg.norm(radius_of_curvature))

    xs_t = [new_points[0][i], new_points[0][i] + d1[0]]
    ys_t = [new_points[1][i], new_points[1][i] + d1[1]]
    zs_t = [new_points[2][i], new_points[2][i] + d1[2]]

    xs_c = [new_points[0][i], new_points[0][i] + radius_of_curvature[0]]
    ys_c = [new_points[1][i], new_points[1][i] + radius_of_curvature[1]]
    zs_c = [new_points[2][i], new_points[2][i] + radius_of_curvature[2]]

    ax.plot(xs_t, ys_t, zs_t, 'g-')
    ax.plot(xs_c, ys_c, zs_c, 'b-')

plt.show()