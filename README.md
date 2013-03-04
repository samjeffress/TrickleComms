SmsScheduler
============

Used for sending SMS to customers:

* Trickling a number of SMS over a period
* Trickling a number of SMS with a defined time gap
* Scheduling of individual SMS
* Pausing / resuming scheduling of SMS
* Directly sending SMS

To get up and running run the Setup.bat in the base folder. This will build, test, setup infrastructure and install everything that you need. 

If you want to integrate the SmsScheduler into your build server & deployment scripts you can access the powershell scripts
yourself (SmsScheduler/Installer.ps1) or there are a few batch files that will do the basic tasks for you. Obviously you'll need to sort out 
configuration, but this is already started on the web project.

If you find any problems please feel free to contribute, we'd love any help / suggestions / code reviews.

Technologies used:
* [NServiceBus](http://www.nservicebus.com) for reliable messaging
* [Twilio](http://www.twilio.com) for SMS delivery
* [Mailgun](http://www.mailgun.com) for Email delivery
* [RavenDB](http://www.ravendb.com) for document storage

To use in production you will need to obtain licences for each technology, but some free versions are available (NServiceBus and Mailgun).

If you want to change the SMS or Email providers, this should be very easy to do.

To extend the usages of events (e.g. a MessageSent event), just subscribe to the message and save it into your CRM etc.

Please contact me if you've got any questions!

If you get warnings about the .net version being higher than powershell you'll need to create some config files - http://tfl09.blogspot.com.au/2010/08/using-newer-versions-of-net-with.html.
