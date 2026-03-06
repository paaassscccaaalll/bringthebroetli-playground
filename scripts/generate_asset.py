import struct

def write_vox(filename, size, voxels):
    sx, sy, sz = size

    def chunk(id, content, children=b''):
        return id.encode('ascii') + struct.pack('<II', len(content), len(children)) + content + children

    size_chunk = chunk('SIZE', struct.pack('<III', sx, sy, sz))
    xyzi_data = struct.pack('<I', len(voxels))
    for (x, y, z, c) in voxels:
        xyzi_data += struct.pack('BBBB', x, y, z, c)
    xyzi_chunk = chunk('XYZI', xyzi_data)

    def color(r, g, b):
        return (0xFF << 24) | (b << 16) | (g << 8) | r

    palette = [0] * 256
    palette[1]  = color(210, 160, 120)  # skin
    palette[2]  = color(180, 100,  50)  # beard
    palette[3]  = color(230, 220, 190)  # cream hat
    palette[4]  = color( 50, 100,  50)  # green vest
    palette[5]  = color(240, 230, 200)  # cream shirt
    palette[6]  = color(100,  60,  30)  # brown pants
    palette[7]  = color( 60,  35,  20)  # dark boots
    palette[8]  = color(160, 110,  60)  # tan backpack
    palette[9]  = color( 90, 120, 150)  # blue backpack detail
    palette[10] = color(190, 150,  80)  # wicker basket
    palette[11] = color(220, 180,  80)  # golden bread
    palette[12] = color(180,  40,  30)  # red neckerchief
    palette[13] = color(160,  80,  40)  # red-brown hair
    palette[14] = color(120,  80,  40)  # dark backpack strap
    palette[15] = color( 80,  50,  20)  # belt

    palette_data = b''
    for v in palette:
        palette_data += struct.pack('BBBB', v&0xFF, (v>>8)&0xFF, (v>>16)&0xFF, (v>>24)&0xFF)
    rgba_chunk = chunk('RGBA', palette_data)

    main_children = chunk('PACK', struct.pack('<I', 1)) + size_chunk + xyzi_chunk + rgba_chunk
    main_chunk = chunk('MAIN', b'', main_children)

    with open(filename, 'wb') as f:
        f.write(b'VOX ')
        f.write(struct.pack('<I', 150))
        f.write(main_chunk)
    print(f"Done! {len(voxels)} voxels -> {filename}")


def box(vs, x0, y0, z0, x1, y1, z1, c):
    for x in range(x0, x1+1):
        for y in range(y0, y1+1):
            for z in range(z0, z1+1):
                vs.add((x, y, z, c))

def hollow(vs, x0, y0, z0, x1, y1, z1, c):
    for x in range(x0, x1+1):
        for y in range(y0, y1+1):
            for z in range(z0, z1+1):
                if x in (x0,x1) or y in (y0,y1) or z in (z0,z1):
                    vs.add((x, y, z, c))

v = set()

# Boots
box(v, 30,35,2,  37,45,8,  7)
box(v, 43,35,2,  50,45,8,  7)
# Legs/pants
box(v, 30,35,8,  37,45,25, 6)
box(v, 43,35,8,  50,45,25, 6)
# Belt
box(v, 28,33,25, 52,47,27, 15)
# Torso (shirt)
box(v, 28,33,27, 52,47,50, 5)
# Green vest
box(v, 30,33,28, 50,37,50, 4)
box(v, 30,33,28, 33,47,50, 4)
box(v, 47,33,28, 50,47,50, 4)
# Red neckerchief
box(v, 32,34,47, 48,46,51, 12)
# Arms
box(v, 18,35,28, 28,43,46, 5)
box(v, 52,35,28, 62,43,46, 5)
# Hands
box(v, 16,36,20, 27,43,28, 1)
box(v, 53,36,20, 62,43,28, 1)
# Basket (wicker hollow + bread)
hollow(v, 10,33,10, 27,48,26, 10)
box(v,   10,33,10, 27,48,12, 10)
box(v,   12,35,13, 25,46,20, 11)
box(v,   13,36,18, 24,45,24, 11)
# Head (skin)
box(v, 29,34,51, 51,46,65, 1)
# Hair
box(v, 29,34,54, 29,46,65, 13)
box(v, 51,34,54, 51,46,65, 13)
box(v, 29,44,54, 51,46,65, 13)
# Beard
box(v, 31,33,51, 49,43,59, 2)
# Brows (stern look)
box(v, 32,33,62, 38,36,64, 2)
box(v, 42,33,62, 48,36,64, 2)
# Chef hat brim
box(v, 26,31,65, 54,49,68, 3)
# Chef hat crown
box(v, 30,34,68, 50,46,79, 3)
# Backpack body
box(v, 30,45,28, 50,62,52, 8)
# Backpack blue pocket
box(v, 33,55,38, 47,62,50, 9)
# Backpack straps
box(v, 30,45,28, 33,48,50, 14)
box(v, 47,45,28, 50,48,50, 14)

voxel_list = [(x,y,z,c) for (x,y,z,c) in v if 0<=x<80 and 0<=y<80 and 0<=z<80]
write_vox('baker_character.vox', (80,80,80), voxel_list)