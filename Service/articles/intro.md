---
title: "How to use the Rig microservice?"
output: html_document
---

Typical Usage
===
1. Upload a new Rig using the `Post` web api method.
2. Call the `Get` method with the identifier of the uploaded Rig as argument. 
The return Json object contains the Rig description.
3. Optionally send a `Delete` request with the identifier of the Rig in order to delete the Rig if you do not 
want to keep the Rig uploaded on the microservice.


