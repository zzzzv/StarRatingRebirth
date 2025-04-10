# ��python��calculate����SR֮ǰ���dump_locals(locals())�����浱ǰ�ֲ�������json�ļ�

import json
import numpy as np

class NumpyEncoder(json.JSONEncoder):
    def default(self, obj):
        if isinstance(obj, np.ndarray):
            return obj.tolist()
        elif isinstance(obj, np.integer):
            return int(obj)
        elif isinstance(obj, np.floating):
            return float(obj)
        elif isinstance(obj, np.bool_):
            return bool(obj)
        elif isinstance(obj, set):
            return list(obj)
        elif hasattr(obj, 'tolist'):
            return obj.tolist()
        return super(NumpyEncoder, self).default(obj)


def dump_locals(local_dict):
    json_file = local_dict['file_path'].replace('.osu', '.json')
    with open(json_file, 'w') as f:
        vars = [
            'K', 'T', 'x', 'note_seq', 'note_seq_by_column', 'LN_seq', 'tail_seq', 'LN_seq_by_column',
            'all_corners', 'base_corners', 'A_corners', 'key_usage', 'active_columns', 'key_usage_400', 
            'anchor', 'delta_ks', 'Jbar', 'Xbar', 'LN_rep', 'Pbar', 'Abar', 'Rbar', 'C_arr', 'Ks_arr',
        ]
        data = {var: local_dict[var] for var in vars if var in local_dict}
        json.dump(data, f, indent=2, cls=NumpyEncoder)