API Documentation with all working endpoints:

markdown
# AuthService API Documentation

## Base URL
- **Local Development:** `http://localhost:5000`
- **HTTPS:** `https://localhost:7001`

## Authentication
Most admin endpoints require JWT authentication. After login, include the token in the Authorization header:
Authorization: Bearer your-jwt-token-here

text

---
## Health Check Endpoints (No Auth Required)
### 1. GET `/api/v1/health`
**Description:** Basic service health check - verifies the API is running

**Response Example:**
```json
{
  "status": "healthy",
  "service": "AuthService",
  "timestamp": "2026-05-20T21:14:52.8600865Z",
  "version": "1.0.0"
}

2. GET /api/v1/health/ping
Description: Simple ping endpoint - fastest way to test if service is responding
Response Example:

json
{
  "message": "pong",
  "timestamp": "2026-05-20T21:15:58.1020879Z"
}

3. GET /api/v1/health/db
Description: Database connectivity check - verifies PostgreSQL (Neon) connection
Response Example (Success):

json
{
  "status": "healthy",
  "database": "connected",
  "timestamp": "2026-05-20T21:12:29.1285924Z",
  "testQuery": "success"
}
Response Example (Failure):

json
{
  "status": "unhealthy",
  "database": "disconnected",
  "timestamp": "2026-05-20T21:12:29.1285924Z"
}

Admin Authentication Endpoints
4. POST /api/v1/admins/register
Description: Register a new admin account (requires admin key)
Request Body:

json
{
  "username": "adminuser",
  "email": "admin@example.com",
  "password": "SecurePass123!",
  "confirmPassword": "SecurePass123!",
  "adminKey": "SUPER_SECRET_ADMIN_KEY_2024"
}

Password Requirements:
Minimum 8 characters
At least one uppercase letter
At least one lowercase letter
At least one number
At least one special character
Response Example (Success):

json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "adminId": "11adabcd-664f-4c08-94f2-400b603d4bef",
  "message": "Admin registered successfully"
}

Error Responses:
400 Bad Request - Invalid input or username/email already exists
403 Forbidden - Invalid admin key provided

5. POST /api/v1/admins/login
Description: Login as an admin (returns JWT token)
Request Body:

json
{
  "username": "adminuser",
  "password": "SecurePass123!"
}
Note: You can use either username or email in the username field
Response Example (Success):

json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "adminId": "11adabcd-664f-4c08-94f2-400b603d4bef",
  "username": "adminuser",
  "email": "admin@example.com",
  "message": "Login successful"
}

Error Responses:
400 Bad Request - Invalid input format
401 Unauthorized - Invalid username/email or password

6. POST /api/v1/admins/logout
Description: Logout an admin (requires authentication)
Headers Required:

text
Authorization: Bearer your-jwt-token
Response Example:

json
{
  "message": "Logout successful"
}

7. GET /api/v1/admins/profile
Description: Get admin profile details (requires authentication)
Headers Required:

text
Authorization: Bearer your-jwt-token
Response Example:

json
{
  "id": "11adabcd-664f-4c08-94f2-400b603d4bef",
  "username": "adminuser",
  "email": "admin@example.com",
  "role": "Admin",
  "createdAt": "2026-05-21T14:12:10.4556Z",
  "lastLoginAt": "2026-05-21T15:30:00Z",
  "updatedAt": null,
  "avatarUrl": "https://res.cloudinary.com/..."
}

8. PUT /api/v1/admins/profile
Description: Update admin profile (username or email) - requires authentication
Headers Required:

text
Authorization: Bearer your-jwt-token
Request Body:

json
{
  "username": "newusername",
  "email": "newemail@example.com"
}
Note: Both fields are optional - send only what you want to update

Response Example:

json
{
  "message": "Profile updated successfully",
  "username": "newusername",
  "email": "newemail@example.com"
}

9. POST /api/v1/admins/avatar
Description: Upload admin profile image (requires authentication)
Headers Required:

text
Authorization: Bearer your-jwt-token
Content-Type: multipart/form-data
Form Data:

Key: file (or avatar depending on your Postman setup)
Value: Select image file (JPEG, PNG, WEBP, max 5MB)
Response Example:

json
{
  "message": "Avatar uploaded successfully",
  "avatarUrl": "https://res.cloudinary.com/debbpghel/image/upload/v1234567890/admin_avatars/xxxxx.png"
}

Swagger UI (Interactive Documentation)
GET /swagger
Description: Interactive API documentation page where you can test all endpoints

Browser: http://localhost:5000/swagger
Features:
Click "Authorize" button to enter your JWT token
All authenticated endpoints show a lock icon 🔒
Click "Try it out" to test endpoints directly

Quick Test Links
No Auth Required:
http://localhost:5000/api/v1/health
http://localhost:5000/api/v1/health/ping
http://localhost:5000/api/v1/health/db
http://localhost:5000/swagger

Authentication Required (use Swagger "Authorize" button):
POST /api/v1/admins/logout
GET /api/v1/admins/profile
PUT /api/v1/admins/profile
POST /api/v1/admins/avatar

Delete Account - Test Endpoints
Step 1: Login first (get token)
bash
curl -X POST http://localhost:5000/api/v1/admins/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "alexander",
    "password": "ResetPass789!@#"
  }'
Save the token from response.

Test 1: Check Account Status (Before Deletion)
bash
curl -X GET http://localhost:5000/api/v1/admins/account/status \
  -H "Authorization: Bearer YOUR_TOKEN"
Expected Response:

json
{
  "isDeleted": false,
  "deletedAt": null,
  "permanentDeleteAt": null,
  "canBeRestored": false,
  "deleteReason": null
}
Test 2: Soft Delete (30 days reversible)
bash
curl -X DELETE http://localhost:5000/api/v1/admins/account \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "confirmUsername": "alexander",
    "permanentDelete": false,
    "reason": "Taking a break from the platform"
  }'
Expected Response:

json
{
  "message": "Account scheduled for permanent deletion in 30 days. You can restore your account anytime before then.",
  "permanentDeleteDate": "2026-06-21T18:00:00Z",
  "isReversible": true
}
Test 3: Check Account Status (After Soft Delete)
bash
curl -X GET http://localhost:5000/api/v1/admins/account/status \
  -H "Authorization: Bearer YOUR_TOKEN"
Expected Response:

json
{
  "isDeleted": true,
  "deletedAt": "2026-05-22T18:00:00Z",
  "permanentDeleteAt": "2026-06-21T18:00:00Z",
  "canBeRestored": true,
  "deleteReason": "Taking a break from the platform"
}
Test 4: Restore Account (Within 30 days)
bash
curl -X POST http://localhost:5000/api/v1/admins/account/restore \
  -H "Authorization: Bearer YOUR_TOKEN"
Expected Response:

json
{
  "message": "Account restored successfully"
}
Test 5: Check Status After Restore
bash
curl -X GET http://localhost:5000/api/v1/admins/account/status \
  -H "Authorization: Bearer YOUR_TOKEN"
Expected Response:

json
{
  "isDeleted": false,
  "deletedAt": null,
  "permanentDeleteAt": null,
  "canBeRestored": false,
  "deleteReason": null
}
Test 6: Hard Delete (Immediate Permanent - CANNOT undo)
⚠️ WARNING: This will permanently delete the account and avatar. Cannot be undone!

bash
curl -X DELETE http://localhost:5000/api/v1/admins/account \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "confirmUsername": "alexander",
    "permanentDelete": true
  }'
Expected Response:

json
{
  "message": "Account permanently deleted. This action cannot be undone.",
  "permanentDeleteDate": null,
  "isReversible": false
}
Test 7: Try to Login After Hard Delete (Should Fail)
bash
curl -X POST http://localhost:5000/api/v1/admins/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "alexander",
    "password": "ResetPass789!@#"
  }'
Expected Response:

json
{
  "message": "Invalid username/email or password"
}
Summary Table:
Test	Endpoint	Method	What it does
1	/api/v1/admins/account/status	GET	Check deletion status
2	/api/v1/admins/account	DELETE	Soft delete (30 days)
3	/api/v1/admins/account/status	GET	Verify soft delete
4	/api/v1/admins/account/restore	POST	Restore account
5	/api/v1/admins/account/status	GET	Verify restore
6	/api/v1/admins/account	DELETE	Hard delete (permanent)
7	/api/v1/admins/login	POST	Verify cannot login
Important Notes:
Delete Type	Reversible	Avatar Deleted	Can Login Again
Soft Delete	✅ Yes (30 days)	❌ No	❌ No
Hard Delete	❌ No	✅ Yes	❌ No (account gone)
