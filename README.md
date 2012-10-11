SmsScheduler
============

Used for sending SMS to customers:

* Trickling a number of SMS over a period
* Trickling a number of SMS with a defined time gap
* Scheduling of individual SMS
* Pausing / resuming scheduling of SMS
* Directly sending SMS

Based on NServiceBus with Twilio as the SMS provider.

This is designed to be easily plugged into any system to send messages, and also through subscription to messages that are sent, you can pluging to a system to record customer interactions.