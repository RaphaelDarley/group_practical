import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
from sklearn.decomposition import PCA
import matplotlib

# Function to transform each line into a dictionary with index and embeddings
def transform(line):
    line_ = line.split()
    return {"index": line_[0], "emb": np.array([float(x) for x in line_[1:]])}

# Read the data
with open('stock2vec/sentences.refined.vectors.txt') as f:
    lines = f.readlines()
    stocks = [transform(line) for line in lines]

# Extract embeddings and stock IDs
ids = [stock["index"] for stock in stocks]
embeddings = np.array([stock["emb"] for stock in stocks])

# Perform PCA to reduce to 3D
pca = PCA(n_components=3)
reduced_embeddings = pca.fit_transform(embeddings)

# Plot in 3D
fig = plt.figure(figsize=(10, 7))
ax = fig.add_subplot(111, projection='3d')

# Scatter plot with labels
ax.scatter(reduced_embeddings[:, 0], reduced_embeddings[:, 1], reduced_embeddings[:, 2], marker='o', s=40, c='b')

# Annotate each point with its stock ID
for i, stock_id in enumerate(ids):
    ax.text(reduced_embeddings[i, 0], reduced_embeddings[i, 1], reduced_embeddings[i, 2], stock_id, fontsize=8)

# Labels and title
ax.set_xlabel('PCA 1')
ax.set_ylabel('PCA 2')
ax.set_zlabel('PCA 3')
ax.set_title('3D PCA Visualization of Stock Embeddings')

matplotlib.use('Qt5Agg')
plt.show()
