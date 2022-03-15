from cmath import pi
import socket
import struct

UDP_RECEIVE_MULTICAST_IP = "234.1.1.2"
UDP_RECEIVE_PORT = 15150
UDP_RECEIVE_BYTES = 1024
interface_ip    = "192.168.1.10"

UDP_SEND_IP = '192.168.137.236'
UDP_SEND_PORT = 53941
UDP_SEND_BYTES = 6*8

sock_da42_pos = socket.socket(socket.AF_INET, # Internet
                     socket.SOCK_DGRAM, socket.IPPROTO_UDP) # UDP
sock_da42_pos.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
sock_da42_pos.bind(('', UDP_RECEIVE_PORT))
# Tell the operating system to add the socket to the multicast group
# on all interfaces.
group = socket.inet_aton(UDP_RECEIVE_MULTICAST_IP)

mreq = struct.pack('4s4s', group, socket.inet_aton(interface_ip))
sock_da42_pos.setsockopt(socket.IPPROTO_IP, socket.IP_ADD_MEMBERSHIP, mreq)
sock_da42_pos.setblocking(0)

sock_send_sim_pos = socket.socket(socket.AF_INET, # Internet
                     socket.SOCK_DGRAM) # UDP


while True:
    #msg_bytes, addr = sock_da42_pos.recvfrom(UDP_RECEIVE_BYTES)
    try:
        msg_bytes = sock_da42_pos.recv(UDP_RECEIVE_BYTES)
        [lat_rad, lon_rad] = struct.unpack('<2d', msg_bytes[48:64])
        [alt_m] = struct.unpack('<1f', msg_bytes[64:68])
        [theta_rad, phi_rad, psi_rad] = struct.unpack('<3f', msg_bytes[84:96])
        send_pos_msg=[0, lat_rad/60/180*pi, lon_rad/60/180*pi,alt_m, phi_rad, theta_rad, psi_rad]
        sock_send_sim_pos.sendto(struct.pack('<B6d',*send_pos_msg), (UDP_SEND_IP, UDP_SEND_PORT))
    except BlockingIOError:
            continue
