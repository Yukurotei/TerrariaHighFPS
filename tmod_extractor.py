import os, struct, zlib, sys

def read_7bit_encoded_int(f):
    res = 0
    shift = 0
    while True:
        b = f.read(1)
        if not b: break
        b = ord(b)
        res |= (b & 0x7F) << shift
        if not (b & 0x80): break
        shift += 7
    return res

def read_string(f):
    length = read_7bit_encoded_int(f)
    return f.read(length).decode('utf-8')

def extract_tmod(file_path, output_dir):
    with open(file_path, 'rb') as f:
        f.read(4) # TMOD
        read_string(f) # TML version
        f.read(20) # hash
        f.read(256) # signature
        f.read(4) # data length
        read_string(f) # mod name
        read_string(f) # mod version
        file_count = struct.unpack('<i', f.read(4))[0]
        
        entries = []
        for _ in range(file_count):
            name = read_string(f)
            u_len = struct.unpack('<i', f.read(4))[0]
            c_len = struct.unpack('<i', f.read(4))[0]
            entries.append({'name': name, 'u_len': u_len, 'c_len': c_len})
            
        for entry in entries:
            data = f.read(entry['c_len'])
            out_path = os.path.join(output_dir, entry['name'].replace('\\', os.sep))
            os.makedirs(os.path.dirname(out_path), exist_ok=True)
            
            if entry['c_len'] != entry['u_len']:
                try: decompressed = zlib.decompress(data, -15)
                except: decompressed = data
            else: decompressed = data
                
            with open(out_path, 'wb') as out_f: out_f.write(decompressed)

extract_tmod(sys.argv[1], sys.argv[2])
