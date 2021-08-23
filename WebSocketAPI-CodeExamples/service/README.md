# Common account setup
TODO: Add documentation links

## Prerequisites
1. You need [Node.js](https://nodejs.org/) v14 or later.
2. You need Das.WorkItemSigner.

## Setup
When calling Forge Design Automation directly from an untrusted client device (e.g. browser or console app) it is necessary to take additional precautions. See docs.

The following steps should be done in terminal in the `service` folder:

1. Generate private/public key pair
```
dotnet Das.WorkitemSigner.dll generate secret.json
```
2. Export the public key
```
dotnet Das.WorkitemSigner.dll export secret.json public.json
```

You should now have `secret.json` and `public.json` in the `service` folder.

The final step is to upload public.json to Forge Design Automation so it can use it to validate your signatures.

The javascript module `upload_publickey.js` will do this for you but you first need to supply your forge credentials. 

3. Create a `.env` file in `WebSocketAPI-CodeExamples` folder.
```
FORGE_CLIENT_ID=<copy your forge client id here>
FORGE_CLIENT_SECRET=<copy your forge client secret here>
FORGE_CALLBACKURL=http://localhost:3000/browser/demo.html
```

4. Upload  your public key 
```
node upload_publickey.js
```
