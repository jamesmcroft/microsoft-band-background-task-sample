# Microsoft Band Background Task Sample
This code sample is part of my [Developing for Microsoft Band with WinRT] (http://jamescroft.co.uk/category/blog/iot/) blog post series focussing on developing a mobile application for Windows with the Microsoft Band.

This sample looks at creating and registering a background task with a Windows Phone 8.1 application that utilises the Band's sensors to gather data periodically for an [indefinite](https://www.bing.com/search?q=define+indefinite&FORM=EDGENN) amount of time. This, for example, allows developers to create innovative running app solutions which can run under the lock screen while still accessing the features of the Microsoft Band.

The background task created in this sample takes advantage of the DeviceUseTrigger which, when registered with your Microsoft Band, allows the task to run for as long as the phone it's running on will allow it. To increase the length of time that the background task can run, prompt the user of the app to open the Battery Saver application and turn on the option to *Allow app to run in the background* and also check the box which reads *Allow this app to run in the background even when Battery Saver is on*.

Please leave any questions or feedback and I will try to get back in touch as soon as possible. If you find an errors or ways to improve the code, feel free to correct them! You can also reach out to me on Twitter [@jamesmcroft](http://www.twitter.com/jamesmcroft)!
