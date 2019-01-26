# MasterController-v2-Suite
This is very messy. :/

The purpose of this software is to generate binary files and upload them to "offline controllers."
In order to perform that action a program called the "Master Controller Interface" is used to add, manage, and delete card NUIDs and users
stored in a MySQL database. In its current form it also exposes other firmware features which can be used to configure
and maintain the system. It can set the RTC and control the "override" state of the door control outputs on the "offline controllers."
In addition to that two monitor applications are integrated to control other devices. One of them is called the "expander."
That monitor application is used for fine grain control over the state of relays in that device and also provides a realtime view of the
output of various sensors integrated into it. The other monitor is used to control the state of relays attached to a WiFi door
controller.

The goal of this project is to encapsulate all the tools necessary to manage a building control system. As of now that
objective has not been reached and more work (specifically, a new hardware revision of the controllers) will be necessary.
The MCI in its current form is fairly robust when used with the "offline controllers" despite them being antiquated in my opinion.
If more work was put in, this software could be very flushed out and many potentially untrusted users could access and manipulate
system data using it. That being said, for now I would suggest only using it on premisses and behind a firewall. Bringup is another
issue as it was written with the intention of never having an empty list of users or a database with no tables. Some checks are
in place but the application will crash when faced with that. You can however restore from a backup via the login form without issue.
The first order of business in terms of making this usable for more people would be fixing that issue and possibly adding a wizard
for system bringup. The issue of card management is also a big one that needs to be addressed. As of now no data other than the card
NUID and a system generated UID are stored for each card. In addition to that I believe that all of the UID generation code should be
replaced with refrences to the C# GUID generation method and the database code should be updated to reflect the byte array generated by that.
