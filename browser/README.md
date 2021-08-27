# Browser Sample

This is a simple html page that show how to use Forge Design Automation Websockets from a browser. 
The sample displays a text box and a button. When you press the button the sample will attempt to submit the contents of the text box as a workitem to Forge Design Automation. The workitem submission is done via websocket so the `demo.html` will receive messages from the service and display these to the user.

## Prerequisites

1. Register a Forge App following the steps [here](https://forge.autodesk.com/developer/start-now/getaccess). For *callback url* use http://localhost:3000/browser/demo.html.
2. You must make your public key available to the Forge Design Automation service by following the steps [here](../service/README.md).
3. Ensure you have [VSCode](https://code.visualstudio.com/) installed.
4. Ensure you have [Live Preview](https://marketplace.visualstudio.com/items?itemName=ms-vscode.live-server) extension installed. This is used to automatically host `demo.html` on `localhost`. You could use any other hosting solution for static web pages (e.g. Microsoft Azure or Amazon Web Services). If you choose to use a different solution then make sure that the *callback url* that you specified in step 1 is updated accordingly.

## Running the sample

1. Open VSCode.
2. Choose `File/Open Folder...` menu and the select the folder where you copied this repo.
3. Choose `View/Command Palette...` menu and run `Live Preview: Start Server` command.
3. Choose `Run/Run Without Debugging...` menu and choose the `Browser Demo` configuration.

VSCode will automatically host demo.html at the http://localhost:3000/browser/demo.html address and launch your browser. `demo.html` immediately redirects to Autodesk for login. This redirect is necessary to obtain a [3-legged token](https://forge.autodesk.com/en/docs/oauth/v2/tutorials/get-3-legged-token-implicit/) that the page uses to authenticate the calls it makes to Forge Design Automation. The Autodesk login page will redirect the browser back to `demo.html` (provided that you setup your *callback url* correctly). The page then waits for the user to press the `Submit` button. 

Try typing "hello" (with quotes) and then press `Submit`. You will see your outgoing message and the following incoming message:
```
← {"action":"error","data":"Invalid payload. Error converting value \"hello\" to type 'Autodesk.Das.Shared.Models.WorkItem'. Path 'data', line 1, position 801."}
```
"hello" is not a valid workitem payload so this is good. The communication with the server is working.

You need to produce valid workitem request body. Here's the simplest workitem in Forge Design Automation (this is an activity that does nothing):
```json
{
  "activityId":"Autodesk.Nop+Latest"
}
```
Copy it into the browser and press `Submit`.

You get the following response from the server:
```
← {"action":"error","data":"{\"Signatures.ActivityId\":[\"Argument must be specified when using 3-legged oauth token. (Parameter 'Signatures.ActivityId')\"]}"}
```
The server does not accept this workitem because this usage is unsafe. The caller (i.e. user who is making the call) must prove that the `activityId` they use is indeed allowed by the developer (who is going to pay for this workitem). The caller can attest by producing developer's signature on this `activityId`. Here's how you do it:

1. Ensure that you fulfilled the prerequisite item 2 above by following the steps [here](../service/README.md).
2. If you follow the above steps then you should have file named `secret.json` in the `service` subfolder. Run the following command:
```
Das.WorkItemSigner.exe secret.json Autodesk.Nop+Latest
```
This will produce a long string as output. This is the developer's signature on the activityId `Autodesk.Nop+Latest`. Use it like this in your browser:
```json
{
  "activityId":"Autodesk.Nop+Latest", 
  "Signatures": {
    "activityId":"<copy signature here>"
  }
}
```
This will produce the following incoming message immediately (note that the dates and ids will be different every time):
```
← {"action":"status","data":{"status":"pending","stats":{"timeQueued":"2021-07-24T17:18:27.5535647Z"},"id":"ba812feb55cd413085ee170fd92123a8"}}
```
followed by another message shortly afterwards:
```
← {"action":"status","data":{"status":"success","reportUrl":"https://dasprod-store.s3.amazonaws.com/workItem/U3kOErNZqmNZ0B4McbgYV9koX27BZDaw/ba812feb55cd413085ee170fd92123a8/report.txt?AWSAccessKeyId=ASIATGVJZKM3GTZ2Z5VC&Expires=1627151008&x-amz-security-token=IQoJb3JpZ2luX2VjEBEaCXVzLWVhc3QtMSJHMEUCIQCEcWPb6IOadHt4yajRLrf6cNdCzfaQFIliLr%2B9MOWJOwIgd%2FhZcbJBdHRiox0USvVRdJEUfdRwaYU3T3gLuuLbuYYqmwIIGhACGgwyMjA0NzMxNTIzMTAiDAB%2F3P100usbLjjQOir4Ad6OuV54TCrjv2VJ232kNnJKgncaZV6N6VAn1FzQkjdaNEyoupXYDB5EHb6luqsXjc0lasDp2vuNOgFBh79nYVAaUSzCTjxEbTWNVbrE%2FNym8eBzEcq28wUb9SnlFUGqktrLm6dZLzk40VFf6fQazNvUn3HUbpAI5m%2FmOp%2BrEf34NzqtrzyIoQg0XZhtULObL1KHG7ZxAvjuFNvDIID0%2Bg3U4Nampyp89SJzPjAIiWF%2BVoRd825IxDsGM%2BNWXVXynBlWjFYiNn4J0jVBu49LgNhzNkzcd8rGfcjk6p4KV3vHnMPMh%2F1dTQiTYI6VJnC6z0wL%2FPzaeeCxMLyI8YcGOpoBomle%2F%2F0DMt8qp58V%2FIQIhPYwZLupAsjABqqBXh7Ni6wFPGOMNj5s2lZMK8NT7rRpN1xvfr%2FFKFjM6bBHCZDs%2FgUTVyaH8FlI3P4%2FObizOUBHAY%2BwWJa0IAeSmPbbtAG03CIRf0I%2FJAr%2BgrJvknpSLjfx1W9Ij%2BvjI67%2BQ4FfD3ZhC5j9PckHAMdSEOGCRB8y1zcP48DG2C3ouQ%3D%3D&Signature=59nlo4UgdbSRgHbAPxDbxkBzjDY%3D","stats":{"timeQueued":"2021-07-24T17:18:27.747Z","timeDownloadStarted":"2021-07-24T17:18:27.8119589Z","timeInstructionsStarted":"2021-07-24T17:18:28.0431301Z","timeInstructionsEnded":"2021-07-24T17:18:31.4754766Z","timeUploadEnded":"2021-07-24T17:18:31.8700172Z","bytesDownloaded":100,"bytesUploaded":100},"id":"ba812feb55cd413085ee170fd92123a8"}}
```
The first message tells us that the server accepted the workitem and the second message tells us when it is complete.
## Walkthrough

The code has been made as simple as possible. The interesting action happens in the `onSubmitHandler` function:
```js
async function onSubmitHandler()
{
  const exceptions = document.getElementById("exceptions");
  exceptions.innerText="";
  try {
    //get workitem content
    const workitem = getInputArea().value;
    //generate a message from it
    const msg = generateMessage(workitem);
    if (!ws) {
      // connect to websocket and hook up a printing message handler
      ws = await connect(e => { printMessage(e.data, true);});
      // if the connection is closed (e.g. due to timeout) then make sure we re-init next time
      ws.onclose = (e) => ws = undefined;
    }
    //send websocket message
    ws.send(msg);
    //print outgoing message
    printMessage(msg, false);
  } 
  catch (e)
  {
    exceptions.innerText= `${e}\n`;
  }
}
```
The workitem content must be wrapped with some additional attributes to create well-formed websocket message:
```js
function generateMessage(workitem)
{
    const message = {
      action : "post-workitem",
      headers : { 
        Authorization: `${params.token_type} ${params.access_token}`
      },
      data: JSON.parse(workitem)
    }
    return JSON.stringify(message);
}
```
Finally, the websocket connection is established using this simple function:
```js
async function connect(onmessage)
{
  return new Promise((resolve, reject) => {
    const ws = new WebSocket("wss://websockets.forgedesignautomation.io");
    ws.onmessage = onmessage;
    ws.onopen = e => resolve(ws)
    ws.onerror = e => reject(e.data)
  });
}
```


