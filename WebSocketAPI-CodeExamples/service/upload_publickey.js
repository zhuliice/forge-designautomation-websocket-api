import axios from "axios"
import dotenv from "dotenv"
import fs from "fs"

dotenv.config();

async function getForgeToken() {
  // get 2-legged
  const params = new URLSearchParams({
      client_id: process.env.FORGE_CLIENT_ID,
      client_secret: process.env.FORGE_CLIENT_SECRET,
      grant_type: 'client_credentials',
      scope: 'code:all'
  });

  const config = {
      headers: {
      'Content-Type': 'application/x-www-form-urlencoded'
      }
  }

  const response = await axios.post('https://developer.api.autodesk.com/authentication/v1/authenticate', params, config);
  return response.data.access_token;
}

const publicKey = JSON.parse(await fs.promises.readFile(new URL("./public.json",import.meta.url)));
const token = await getForgeToken();
await axios.patch('https://developer.api.autodesk.com/da/us-east/v3/forgeapps/me', {publicKey: publicKey} ,{headers: {"Authorization" : `Bearer ${token}`}});
console.log("Public key has been uploaded.")
