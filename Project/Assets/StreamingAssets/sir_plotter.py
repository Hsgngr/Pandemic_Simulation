import numpy as np
import os
import matplotlib.pyplot as plt

with open('sir_data.txt', 'r') as f:
    data = list(map(lambda x: x.replace(',', '.'), f.readlines()))

filtered_data = []
for datum in data:
    filtered_data.append(datum[:-1].split(';'))

data_arr = np.flip(np.array(filtered_data, dtype=float), axis=1)
dt = (data_arr[1:, 0] - data_arr[:-1, 0]).mean()
total_subject = data_arr[0, 1:].sum()

initial_count = 1000
a = []
a.append(list(data_arr[0]))
a = np.array(a * initial_count)
a[:, 0] = np.arange(-initial_count, 0) * dt
extended_data_arr = np.insert(data_arr, 0, a, axis=0)

fig, ax = plt.subplots(1, 1, figsize=(8, 4))
plt.ion()
fig.show()
fig.canvas.draw()

dt_draw = 20
fig_dir = 'figures'
if not os.path.isdir(fig_dir):
    os.mkdir(fig_dir)
for idx, i in enumerate(np.arange(initial_count, len(extended_data_arr), dt_draw)):
    ax.clear()
    # fig.clf()
    ax.fill_between(extended_data_arr[:i, 0], extended_data_arr[:i, 3] + extended_data_arr[:i, 2],
                    extended_data_arr[:i, 2], color='#30616c')
    ax.fill_between(extended_data_arr[:i, 0], extended_data_arr[:i, 2], 0, color='#f56753')
    ax.fill_between(extended_data_arr[:i, 0], total_subject - extended_data_arr[:i, 1], total_subject, color='#444444')
    fig.canvas.draw()
    fig.savefig(os.path.join(fig_dir, str(idx) + '.png'))

os.system('ffmpeg -f image2 -i figures/%d.png video.avi')
