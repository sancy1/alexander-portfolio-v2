cd ~/Desktop/alexander-portfolio-v2

# Show graph with affected filter (new syntax)
npx nx graph --affected

# Run build for specific project (without dry-run)
npx nx run auth-service:build

# Run build for all projects
npx nx run-many --target=build --all

# Run a specific target
npx nx run auth-service:restore

# Run auth service locally
npx nx run auth-service:run


------------------------------------------
cd ~/Desktop/alexander-portfolio-v2

# 1. List all projects (should show auth-service)
npx nx show projects

# 2. Restore packages (this will work)
npx nx run auth-service:restore

# 3. Build the project (this will actually build)
npx nx run auth-service:build-dev

# 4. Run the API locally via Nx
npx nx run auth-service:run


--------------------------------------
Summary: How Nx Fits Your Deployment
-----------------------------------------------------------------------
Stage	        Without Nx	            With Nx
-----------------------------------------------------------------------
Detect changes	Manual or rebuild all	nx affected automatically detects
Build	        Rebuild all 4 services	Only builds changed service(s)
Docker	        Rebuild all images	    Only builds changed images
Time	        10-15 minutes	        2-3 minutes
Cost	        High	                Low


--------------------------------------
cd ~/Desktop/alexander-portfolio-v2
--------------------------------------
# Pull images first (to see download progress)
docker pull postgres:16-alpine
docker pull rabbitmq:4-management-alpine
docker pull redis:7-alpine
docker pull confluentinc/cp-kafka:7.6.0

# Then start all services
docker-compose up -d

# Check if containers are running
docker-compose ps

