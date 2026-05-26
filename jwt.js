// // jwt.js

// const jwt = require("jsonwebtoken");
// const crypto = require("crypto");

// /*
// |--------------------------------------------------------------------------
// | JWT Configuration
// |--------------------------------------------------------------------------
// |
// | Recommended Secret Lengths:
// |
// | Minimum:
// |   - 32 characters (256 bits)
// |
// | Recommended:
// |   - 64+ characters
// |
// | Best Practice:
// |   - Generate cryptographically secure secrets
// |   - Store in environment variables
// |
// */

// // Generate secure secrets if not provided
// const ACCESS_TOKEN_SECRET =
//   process.env.ACCESS_TOKEN_SECRET ||
//   crypto.randomBytes(64).toString("hex");

// const REFRESH_TOKEN_SECRET =
//   process.env.REFRESH_TOKEN_SECRET ||
//   crypto.randomBytes(64).toString("hex");

// // Token expiration settings
// const ACCESS_TOKEN_EXPIRES_IN = "15m";
// const REFRESH_TOKEN_EXPIRES_IN = "7d";

// /*
// |--------------------------------------------------------------------------
// | Validate Secret Strength
// |--------------------------------------------------------------------------
// */

// function validateSecret(secret, name) {
//   if (!secret || secret.length < 32) {
//     throw new Error(
//       `${name} must be at least 32 characters long`
//     );
//   }
// }

// validateSecret(ACCESS_TOKEN_SECRET, "ACCESS_TOKEN_SECRET");
// validateSecret(REFRESH_TOKEN_SECRET, "REFRESH_TOKEN_SECRET");

// /*
// |--------------------------------------------------------------------------
// | Generate Access Token
// |--------------------------------------------------------------------------
// */

// function generateAccessToken(payload) {
//   return jwt.sign(payload, ACCESS_TOKEN_SECRET, {
//     algorithm: "HS256",
//     expiresIn: ACCESS_TOKEN_EXPIRES_IN,
//     issuer: "your-app-name",
//     audience: "your-app-users",
//   });
// }

// /*
// |--------------------------------------------------------------------------
// | Generate Refresh Token
// |--------------------------------------------------------------------------
// */

// function generateRefreshToken(payload) {
//   return jwt.sign(payload, REFRESH_TOKEN_SECRET, {
//     algorithm: "HS256",
//     expiresIn: REFRESH_TOKEN_EXPIRES_IN,
//     issuer: "your-app-name",
//     audience: "your-app-users",
//   });
// }

// /*
// |--------------------------------------------------------------------------
// | Verify Access Token
// |--------------------------------------------------------------------------
// */

// function verifyAccessToken(token) {
//   try {
//     return jwt.verify(token, ACCESS_TOKEN_SECRET, {
//       algorithms: ["HS256"],
//       issuer: "your-app-name",
//       audience: "your-app-users",
//     });
//   } catch (error) {
//     throw new Error(`Invalid access token: ${error.message}`);
//   }
// }

// /*
// |--------------------------------------------------------------------------
// | Verify Refresh Token
// |--------------------------------------------------------------------------
// */

// function verifyRefreshToken(token) {
//   try {
//     return jwt.verify(token, REFRESH_TOKEN_SECRET, {
//       algorithms: ["HS256"],
//       issuer: "your-app-name",
//       audience: "your-app-users",
//     });
//   } catch (error) {
//     throw new Error(`Invalid refresh token: ${error.message}`);
//   }
// }

// /*
// |--------------------------------------------------------------------------
// | Export Functions
// |--------------------------------------------------------------------------
// */

// module.exports = {
//   generateAccessToken,
//   generateRefreshToken,
//   verifyAccessToken,
//   verifyRefreshToken,
// };








// # Using PowerShell to generate random hex
// powershell -Command "[Convert]::ToHexString([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))"








// generate-jwt-secret.js
// Run with: node generate-jwt-secret.js

const crypto = require("crypto");

// Generate a 64-character hex string (256 bits * 2 = 64 chars)
// This is what your C# JWT needs
const jwtSecret = crypto.randomBytes(32).toString("hex");

console.log("\n========== COPY THIS TO YOUR .env FILE ==========\n");
console.log(`JWT_SECRET=${jwtSecret}`);
console.log(`JWT_ISSUER=auth-service`);
console.log(`JWT_AUDIENCE=portfolio-api`);
console.log(`JWT_EXPIRY_MINUTES=60`);
console.log("\n================================================\n");

// Also show a human-readable version if you prefer
const readableSecret = crypto.randomBytes(24).toString("base64");
console.log("Alternative (base64 format - also works):");
console.log(`JWT_SECRET=${readableSecret}\n`);