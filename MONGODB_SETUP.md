# MongoDB Setup Instructions

## Option 1: Docker (Recommended for Development)
```bash
# Start MongoDB with Docker
docker run -d --name autofixer-mongodb \
  -p 27017:27017 \
  -e MONGO_INITDB_ROOT_USERNAME=admin \
  -e MONGO_INITDB_ROOT_PASSWORD=password \
  -e MONGO_INITDB_DATABASE=AutoFixer \
  -v mongodb_data:/data/db \
  mongo:7.0

# Wait for MongoDB to start
docker logs autofixer-mongodb
```

## Option 2: Local Installation
### Windows:
1. Download MongoDB Community Server from https://www.mongodb.com/try/download/community
2. Install and start MongoDB service
3. MongoDB will be available at mongodb://localhost:27017

### macOS:
```bash
brew tap mongodb/brew
brew install mongodb-community@7.0
brew services start mongodb/brew/mongodb-community@7.0
```

### Linux (Ubuntu):
```bash
# Import MongoDB public GPG key
wget -qO - https://www.mongodb.org/static/pgp/server-7.0.asc | sudo apt-key add -

# Create list file for MongoDB
echo "deb [ arch=amd64,arm64 ] https://repo.mongodb.org/apt/ubuntu focal/mongodb-org/7.0 multiverse" | sudo tee /etc/apt/sources.list.d/mongodb-org-7.0.list

# Install MongoDB
sudo apt-get update
sudo apt-get install -y mongodb-org

# Start MongoDB
sudo systemctl start mongod
sudo systemctl enable mongod
```

## Option 3: MongoDB Atlas (Cloud)
1. Create account at https://www.mongodb.com/cloud/atlas
2. Create cluster
3. Get connection string like: mongodb+srv://username:password@cluster.mongodb.net/AutoFixer