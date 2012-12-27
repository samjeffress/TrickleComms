SmsScheduler
============

Used for sending SMS to customers:

* Trickling a number of SMS over a period
* Trickling a number of SMS with a defined time gap
* Scheduling of individual SMS
* Pausing / resuming scheduling of SMS
* Directly sending SMS

Technologies used:
* NServiceBus for reliable messaging
* Twilio for SMS delivery
* Mailgun for Email delivery
* RavenDB for document storage

To use in production you will need to obtain licences for each technology, but some free versions are available (NServiceBus and Mailgun).

If you want to change the SMS or Email providers, this should be very easy to do.

To extend the usages of events (e.g. a MessageSent event), just subscribe to the message and save it into your CRM etc.

Please contact me if you've got any questions!

