Here's the complete updated API Documentation with ALL working endpoints:

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

Key: file

Value: Select image file (JPEG, PNG, WEBP, max 5MB)

Response Example:

json
{
  "message": "Avatar uploaded successfully",
  "avatarUrl": "https://res.cloudinary.com/debbpghel/image/upload/v1234567890/admin_avatars/xxxxx.png"
}
10. PUT /api/v1/admins/change-password
Description: Change admin password (requires current password)

Headers Required:

text
Authorization: Bearer your-jwt-token
Request Body:

json
{
  "currentPassword": "OldPass123!@#",
  "newPassword": "NewPass456!@#",
  "confirmNewPassword": "NewPass456!@#"
}
Response Example:

json
{
  "message": "Password changed successfully"
}
Error Responses:

400 Bad Request - Validation error or current password incorrect

11. POST /api/v1/admins/reset-password
Description: Reset forgotten password using Admin Secret Key (no email required)

Request Body:

json
{
  "username": "adminuser",
  "adminKey": "SUPER_SECRET_ADMIN_KEY_2024",
  "newPassword": "NewPass456!@#",
  "confirmNewPassword": "NewPass456!@#"
}
Response Example:

json
{
  "message": "Password reset successfully"
}
Account Management Endpoints
12. GET /api/v1/admins/account/status
Description: Check account deletion status (requires authentication)

Headers Required:

text
Authorization: Bearer your-jwt-token
Response Example (Active Account):

json
{
  "isDeleted": false,
  "deletedAt": null,
  "permanentDeleteAt": null,
  "canBeRestored": false,
  "deleteReason": null
}
Response Example (Soft Deleted):

json
{
  "isDeleted": true,
  "deletedAt": "2026-05-22T18:00:00Z",
  "permanentDeleteAt": "2026-06-21T18:00:00Z",
  "canBeRestored": true,
  "deleteReason": "Taking a break"
}
13. DELETE /api/v1/admins/account
Description: Delete admin account (Soft delete OR Hard delete)

Headers Required:

text
Authorization: Bearer your-jwt-token
Request Body (Soft Delete - 30 days reversible):

json
{
  "confirmUsername": "adminuser",
  "permanentDelete": false,
  "reason": "Taking a break"
}
Response (Soft Delete):

json
{
  "message": "Account scheduled for permanent deletion in 30 days. You can restore your account anytime before then.",
  "permanentDeleteDate": "2026-06-21T18:00:00Z",
  "isReversible": true
}
Request Body (Hard Delete - Immediate Permanent):

json
{
  "confirmUsername": "adminuser",
  "permanentDelete": true
}
Response (Hard Delete):

json
{
  "message": "Account permanently deleted. This action cannot be undone.",
  "permanentDeleteDate": null,
  "isReversible": false
}
Error Responses:

400 Bad Request - Username confirmation does not match

14. POST /api/v1/admins/account/restore
Description: Restore a soft-deleted account (only works within 30 days)

Headers Required:

text
Authorization: Bearer your-jwt-token
Response Example:

json
{
  "message": "Account restored successfully"
}
Error Response:

400 Bad Request - Account cannot be restored (not deleted or 30 days passed)

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

Authentication Required:
POST /api/v1/admins/logout

GET /api/v1/admins/profile

PUT /api/v1/admins/profile

POST /api/v1/admins/avatar

PUT /api/v1/admins/change-password

GET /api/v1/admins/account/status

DELETE /api/v1/admins/account

POST /api/v1/admins/account/restore

Endpoint Summary Table
#	Method	Endpoint	Auth Required	Description
1	GET	/api/v1/health	❌	Service health
2	GET	/api/v1/health/ping	❌	Ping test
3	GET	/api/v1/health/db	❌	Database status
4	POST	/api/v1/admins/register	❌	Register admin
5	POST	/api/v1/admins/login	❌	Login admin
6	POST	/api/v1/admins/logout	✅	Logout admin
7	GET	/api/v1/admins/profile	✅	Get profile
8	PUT	/api/v1/admins/profile	✅	Update profile
9	POST	/api/v1/admins/avatar	✅	Upload avatar
10	PUT	/api/v1/admins/change-password	✅	Change password
11	POST	/api/v1/admins/reset-password	❌	Reset password (admin key)
12	GET	/api/v1/admins/account/status	✅	Check deletion status
13	DELETE	/api/v1/admins/account	✅	Delete account
14	POST	/api/v1/admins/account/restore	✅	Restore account
15	GET	/swagger	❌	Swagger UI
Delete Account Comparison
Delete Type	Reversible	Avatar Deleted	Can Login Again
Soft Delete	✅ Yes (30 days)	❌ No	❌ No
Hard Delete	❌ No	✅ Yes	❌ No (account gone)
Last Updated
Date: May 22, 2026
Version: 2.0.0
Status: All 14 API endpoints working, Swagger UI functional, Account deletion (soft/hard) implemented

text

This documentation now includes ALL working endpoints including:
- Change Password
- Reset Password (with admin key)
- Account Status
- Soft Delete (30 days reversible)
- Hard Delete (immediate permanent)
- Restore Account


Add this section to your API Documentation:
markdown
## Social Authentication Endpoints (Google/GitHub)

### 15. GET `/api/v1/auth/google/login`
**Description:** Initiate Google OAuth login flow

**Browser:** `http://localhost:5000/api/v1/auth/google/login`

**How it works:**
1. User clicks this link
2. Redirected to Google consent screen
3. User selects account and approves permissions
4. Redirected back to callback endpoint

---

### 16. GET `/api/v1/auth/google/callback`
**Description:** Google OAuth callback (handled automatically)

**Note:** Frontend does not need to call this directly. It's the redirect URL configured in Google Cloud Console.

**Required Redirect URI in Google Console:**
http://localhost:5000/api/v1/auth/google/callback

text

---

### 17. GET `/api/v1/auth/github/login`
**Description:** Initiate GitHub OAuth login flow

**Browser:** `http://localhost:5000/api/v1/auth/github/login`

**How it works:**
1. User clicks this link
2. Redirected to GitHub authorization screen
3. User approves permissions
4. Redirected back to callback endpoint

---

### 18. GET `/api/v1/auth/github/callback`
**Description:** GitHub OAuth callback (handled automatically)

**Note:** Frontend does not need to call this directly. It's the redirect URL configured in GitHub Developer Settings.

**Required Redirect URI in GitHub:**
http://localhost:5000/api/v1/auth/github/callback

text

---

## Social User Profile Endpoints

### 19. POST `/api/v1/auth/users/complete-registration`
**Description:** Complete social user profile (first time only)

**Query Parameter:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `userId` | GUID | ✅ Yes | The userId returned from OAuth callback |

**Request Body:**
```json
{
  "displayName": "John Doe",
  "avatarUrl": "https://example.com/avatar.jpg"
}
Response Example (Success):

json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "userId": "9af8d9e4-6e43-45be-a0ff-d781526cfc9f",
  "displayName": "John Doe",
  "message": "Profile completed successfully"
}
Response Example (Error):

json
{
  "message": "Display name is required"
}
20. GET /api/v1/auth/users/profile
Description: Get social user profile (requires authentication)

Headers Required:

text
Authorization: Bearer your-jwt-token
Response Example:

json
{
  "id": "9af8d9e4-6e43-45be-a0ff-d781526cfc9f",
  "email": "user@gmail.com",
  "displayName": "John Doe",
  "avatarUrl": "https://lh3.googleusercontent.com/a/...",
  "provider": "Google",
  "isProfileComplete": true,
  "createdAt": "2026-05-22T20:30:37.02279Z",
  "lastLoginAt": "2026-05-22T22:37:24.162565Z"
}
21. PUT /api/v1/auth/users/profile
Description: Update social user display name (requires authentication)

Headers Required:

text
Authorization: Bearer your-jwt-token
Content-Type: application/json
Request Body:

json
{
  "displayName": "New Display Name"
}
Response Example:

json
{
  "message": "Profile updated successfully",
  "displayName": "New Display Name",
  "avatarUrl": "https://lh3.googleusercontent.com/a/..."
}
22. POST /api/v1/auth/users/avatar
Description: Upload custom avatar for social user (requires authentication)

Headers Required:

text
Authorization: Bearer your-jwt-token
Body: form-data

Key	Type	Value
file	File	Select image (JPEG, PNG, WEBP, max 5MB)
Response Example:

json
{
  "message": "Avatar uploaded successfully",
  "avatarUrl": "https://res.cloudinary.com/debbpghel/image/upload/v1779490013/social_user_avatars/xxxxx.png"
}
Social Auth Flow Diagram
text
┌─────────────────────────────────────────────────────────────────────────────┐
│                         SOCIAL LOGIN FLOW                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Step 1: User clicks "Login with Google"                                    │
│          ↓                                                                   │
│  GET /api/v1/auth/google/login                                              │
│          ↓                                                                   │
│  Step 2: Redirect to Google consent screen                                  │
│          ↓                                                                   │
│  Step 3: User approves permissions                                          │
│          ↓                                                                   │
│  Step 4: Redirect to callback with code                                     │
│          ↓                                                                   │
│  Step 5: Backend exchanges code for user info                               │
│          ↓                                                                   │
│  Step 6: Backend checks if user exists                                      │
│                                                                              │
│     ┌─────────────────────┬─────────────────────────────────────┐           │
│     │ NEW USER             │ EXISTING USER                       │           │
│     ├─────────────────────┼─────────────────────────────────────┤           │
│     │ returns:            │ returns:                            │           │
│     │ requiresProfile     │ token + user info                   │           │
│     │ Completion: true    │                                     │           │
│     │ userId: xxx         │                                     │           │
│     └─────────────────────┴─────────────────────────────────────┘           │
│                                                                              │
│  Step 7 (New User Only): POST /api/v1/auth/users/complete-registration      │
│          ↓                                                                   │
│  Step 8: User receives JWT token                                            │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
Social User OAuth Response Examples
First-time Login (New User):
json
{
  "requiresProfileCompletion": true,
  "userId": "9af8d9e4-6e43-45be-a0ff-d781526cfc9f",
  "email": "user@gmail.com",
  "name": "John Doe",
  "avatarUrl": "https://lh3.googleusercontent.com/a/...",
  "provider": "google"
}
Returning User (Profile Complete):
json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "userId": "9af8d9e4-6e43-45be-a0ff-d781526cfc9f",
  "email": "user@gmail.com",
  "displayName": "John Doe",
  "avatarUrl": "https://lh3.googleusercontent.com/a/...",
  "message": "Login successful"
}
Duplicate Email Error (Same email, different provider):
json
{
  "message": "An account with email user@gmail.com already exists. Please login using Google instead."
}
Social User Endpoints Summary Table
#	Method	Endpoint	Auth Required	Description
15	GET	/api/v1/auth/google/login	❌	Initiate Google login
16	GET	/api/v1/auth/google/callback	❌	Google callback (auto)
17	GET	/api/v1/auth/github/login	❌	Initiate GitHub login
18	GET	/api/v1/auth/github/callback	❌	GitHub callback (auto)
19	POST	/api/v1/auth/users/complete-registration	❌ (uses userId)	Complete profile
20	GET	/api/v1/auth/users/profile	✅	Get user profile
21	PUT	/api/v1/auth/users/profile	✅	Update display name
22	POST	/api/v1/auth/users/avatar	✅	Upload custom avatar
What Social Users CAN vs CANNOT Change
Field	Editable?	Method
Display Name	✅ Yes	PUT /api/v1/auth/users/profile
Avatar	✅ Yes	POST /api/v1/auth/users/avatar
Email	❌ No	Locked to OAuth provider
Provider	❌ No	Cannot change login method
Provider ID	❌ No	Tied to OAuth account



Social User Quick Test Links
No Auth Required:
http://localhost:5000/api/v1/auth/google/login

http://localhost:5000/api/v1/auth/github/login

Auth Required (use token from login):
GET http://localhost:5000/api/v1/auth/users/profile

PUT http://localhost:5000/api/v1/auth/users/profile

POST http://localhost:5000/api/v1/auth/users/avatar

Updated Endpoint Summary Table (All Endpoints)
#	Method	Endpoint	Auth Required	Description
1	GET	/api/v1/health	❌	Service health
2	GET	/api/v1/health/ping	❌	Ping test
3	GET	/api/v1/health/db	❌	Database status
4	POST	/api/v1/admins/register	❌	Register admin
5	POST	/api/v1/admins/login	❌	Login admin
6	POST	/api/v1/admins/logout	✅	Logout admin
7	GET	/api/v1/admins/profile	✅	Get admin profile
8	PUT	/api/v1/admins/profile	✅	Update admin profile
9	POST	/api/v1/admins/avatar	✅	Upload admin avatar
10	PUT	/api/v1/admins/change-password	✅	Change admin password
11	POST	/api/v1/admins/reset-password	❌	Reset admin password
12	GET	/api/v1/admins/account/status	✅	Check deletion status
13	DELETE	/api/v1/admins/account	✅	Delete admin account
14	POST	/api/v1/admins/account/restore	✅	Restore admin account
15	GET	/api/v1/auth/google/login	❌	Google login
16	GET	/api/v1/auth/github/login	❌	GitHub login
17	POST	/api/v1/auth/users/complete-registration	❌	Complete social profile
18	GET	/api/v1/auth/users/profile	✅	Get social profile
19	PUT	/api/v1/auth/users/profile	✅	Update social display name
20	POST	/api/v1/auth/users/avatar	✅	Upload social avatar
21	GET	/swagger	❌	Swagger UI



Add these sections to your API Documentation:
markdown
## Social User Account Management Endpoints

### 23. DELETE `/api/v1/auth/users/account`
**Description:** Social user self-delete account (Soft delete OR Hard delete)

**Headers Required:**
Authorization: Bearer your-jwt-token

text

**Request Body (Soft Delete - 30 days reversible):**
```json
{
  "confirmEmail": "user@example.com",
  "permanentDelete": false,
  "reason": "Taking a break"
}
Response (Soft Delete):

json
{
  "message": "Account scheduled for permanent deletion in 30 days. You can restore your account anytime before then by logging in again.",
  "permanentDeleteDate": "2026-06-22T18:00:00Z",
  "isReversible": true
}
Request Body (Hard Delete - Immediate Permanent):

json
{
  "confirmEmail": "user@example.com",
  "permanentDelete": true
}
Response (Hard Delete):

json
{
  "message": "Account permanently deleted. This action cannot be undone.",
  "permanentDeleteDate": null,
  "isReversible": false
}
Error Responses:

400 Bad Request - Email confirmation does not match

24. GET /api/v1/auth/users/account/status
Description: Check social user account deletion status

Headers Required:

text
Authorization: Bearer your-jwt-token
Response Example (Active Account):

json
{
  "isDeleted": false,
  "deletedAt": null,
  "permanentDeleteAt": null,
  "canBeRestored": false,
  "deleteReason": null
}
Response Example (Soft Deleted):

json
{
  "isDeleted": true,
  "deletedAt": "2026-05-23T18:00:00Z",
  "permanentDeleteAt": "2026-06-22T18:00:00Z",
  "canBeRestored": true,
  "deleteReason": "Taking a break"
}
25. POST /api/v1/auth/users/account/restore
Description: Restore a soft-deleted social user account (within 30 days)

Request Body:

json
{
  "email": "user@example.com"
}
Response Example:

json
{
  "message": "Account restored successfully. You can now login again."
}
Error Responses:

400 Bad Request - Account is blocked by admin (cannot self-restore)

400 Bad Request - Account has been permanently deleted

404 Not Found - User not found

Admin Social User Management Endpoints
26. POST /api/v1/admins/admin/social-users/{userId}/block
Description: Admin blocks a social user (prevents self-restore)

Headers Required:

text
Authorization: Bearer your-jwt-token (Admin role required)
URL Parameters:

Parameter	Type	Required	Description
userId	GUID	✅ Yes	ID of the social user to block
Request Body:

json
{
  "reason": "Violation of community guidelines"
}
Response Example:

json
{
  "message": "User has been blocked. User cannot self-restore."
}
27. POST /api/v1/admins/admin/social-users/{userId}/unblock
Description: Admin unblocks a social user (allows self-restore again)

Headers Required:

text
Authorization: Bearer your-jwt-token (Admin role required)
URL Parameters:

Parameter	Type	Required	Description
userId	GUID	✅ Yes	ID of the social user to unblock
Response Example:

json
{
  "message": "User has been unblocked. User can now self-restore if account is soft-deleted."
}
28. DELETE /api/v1/admins/admin/social-users/{userId}
Description: Admin forced delete of social user account

Headers Required:

text
Authorization: Bearer your-jwt-token (Admin role required)
URL Parameters:

Parameter	Type	Required	Description
userId	GUID	✅ Yes	ID of the social user to delete
Request Body (Soft Delete - 30 days):

json
{
  "reason": "Spam account",
  "permanentDelete": false
}
Response (Soft Delete):

json
{
  "message": "User user@example.com has been deactivated. Action: Spam account",
  "permanentDeleteDate": "2026-06-22T18:00:00Z",
  "isReversible": true
}
Request Body (Hard Delete - Permanent):

json
{
  "reason": "Violation of terms",
  "permanentDelete": true
}
Response (Hard Delete):

json
{
  "message": "User user@example.com has been permanently deleted. Action: Violation of terms",
  "permanentDeleteDate": null,
  "isReversible": false
}
Admin Actions on Social Users - Summary Table
Action	Endpoint	Method	Effect
Block User	/api/v1/admins/admin/social-users/{userId}/block	POST	User cannot self-restore
Unblock User	/api/v1/admins/admin/social-users/{userId}/unblock	POST	User can self-restore again
Soft Delete	/api/v1/admins/admin/social-users/{userId}	DELETE	Account deactivated for 30 days
Hard Delete	/api/v1/admins/admin/social-users/{userId}	DELETE	Account permanently deleted
Social User Account Flow Diagram
text
┌─────────────────────────────────────────────────────────────────────────────┐
│                    SOCIAL USER ACCOUNT LIFECYCLE                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────┐                                                           │
│  │  NEW USER    │                                                           │
│  └──────┬───────┘                                                           │
│         │                                                                    │
│         ▼                                                                    │
│  ┌──────────────┐     Complete Profile     ┌──────────────┐                │
│  │ Profile      │ ───────────────────────▶ │ Active       │                │
│  │ Incomplete   │                          │ Account      │                │
│  └──────────────┘                          └──────┬───────┘                │
│                                                    │                         │
│                    ┌───────────────────────────────┼───────────────────────┐│
│                    │                               │                       ││
│                    ▼                               ▼                       ││
│           ┌──────────────┐                ┌──────────────┐                ││
│           │ User Self-   │                │ Admin        │                ││
│           │ Delete       │                │ Delete       │                ││
│           └──────┬───────┘                └──────┬───────┘                ││
│                  │                               │                         ││
│                  ▼                               ▼                         ││
│           ┌──────────────┐                ┌──────────────┐                ││
│           │ Soft Delete  │                │ Admin Block  │                ││
│           │ (30 days)    │                │              │                ││
│           └──────┬───────┘                └──────┬───────┘                ││
│                  │                               │                         ││
│         ┌────────┴────────┐                      │                         ││
│         │                 │                      │                         ││
│         ▼                 ▼                      ▼                         ││
│  ┌──────────────┐  ┌──────────────┐     ┌──────────────┐                  ││
│  │ User Restore │  │ Auto-Permanent│    │ Cannot Self- │                  ││
│  │ (within 30d) │  │ Delete (30d)  │    │ Restore      │                  ││
│  └──────────────┘  └──────────────┘     └──────┬───────┘                  ││
│                                                  │                          ││
│                                                  ▼                          ││
│                                           ┌──────────────┐                ││
│                                           │ Admin        │                ││
│                                           │ Unblock      │                ││
│                                           └──────────────┘                ││
│                                                  │                          ││
│                                                  ▼                          ││
│                                           ┌──────────────┐                ││
│                                           │ Can Self-    │                ││
│                                           │ Restore      │                ││
│                                           └──────────────┘                ││
└─────────────────────────────────────────────────────────────────────────────┘
Updated Endpoint Summary Table (All Endpoints)
#	Method	Endpoint	Auth Required	Description
1	GET	/api/v1/health	❌	Service health
2	GET	/api/v1/health/ping	❌	Ping test
3	GET	/api/v1/health/db	❌	Database status
4	POST	/api/v1/admins/register	❌	Register admin
5	POST	/api/v1/admins/login	❌	Login admin
6	POST	/api/v1/admins/logout	✅	Logout admin
7	GET	/api/v1/admins/profile	✅	Get admin profile
8	PUT	/api/v1/admins/profile	✅	Update admin profile
9	POST	/api/v1/admins/avatar	✅	Upload admin avatar
10	PUT	/api/v1/admins/change-password	✅	Change admin password
11	POST	/api/v1/admins/reset-password	❌	Reset admin password
12	GET	/api/v1/admins/account/status	✅	Check admin deletion status
13	DELETE	/api/v1/admins/account	✅	Delete admin account
14	POST	/api/v1/admins/account/restore	✅	Restore admin account
15	GET	/api/v1/auth/google/login	❌	Google login
16	GET	/api/v1/auth/github/login	❌	GitHub login
17	POST	/api/v1/auth/users/complete-registration	❌	Complete social profile
18	GET	/api/v1/auth/users/profile	✅	Get social profile
19	PUT	/api/v1/auth/users/profile	✅	Update social display name
20	POST	/api/v1/auth/users/avatar	✅	Upload social avatar
21	DELETE	/api/v1/auth/users/account	✅	Social user self-delete
22	GET	/api/v1/auth/users/account/status	✅	Social user deletion status
23	POST	/api/v1/auth/users/account/restore	❌	Social user restore
24	POST	/api/v1/admins/admin/social-users/{userId}/block	✅ (Admin)	Admin block social user
25	POST	/api/v1/admins/admin/social-users/{userId}/unblock	✅ (Admin)	Admin unblock social user
26	DELETE	/api/v1/admins/admin/social-users/{userId}	✅ (Admin)	Admin delete social user
27	GET	/swagger	❌	Swagger UI
Last Updated
Date: May 23, 2026
Version: 4.0.0
Status:

✅ 14 Admin endpoints working

✅ 11 Social User endpoints working

✅ 3 Health endpoints working

✅ 2 OAuth endpoints working

✅ Swagger UI functional

✅ Account deletion (soft/hard) for Admin and Social Users

✅ Admin block/unblock for Social Users

✅ OAuth Google/GitHub integration complete

✅ Avatar upload with Cloudinary for both Admin and Social Users

text

**This completes the API documentation!**
Summary of New Endpoints Added:
#	Method	Endpoint	Description
1	GET	/api/v1/admins/admin/social-users	List all social users (paginated, filterable)
2	GET	/api/v1/admins/admin/social-users/{userId}	Get single user details
3	GET	/api/v1/admins/admin/social-users/blocked/list	List all blocked users
4	GET	/api/v1/admins/admin/social-users/active/recent	List recently active users
5	GET	/api/v1/admins/admin/social-users/stats/summary	Get user statistics