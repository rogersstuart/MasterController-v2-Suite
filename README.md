# MasterController-v2-Suite

MasterController v2 Suite is an offline access control and building device management system. It allows administrators to manage RFID card access (store and sync card UIDs to door controllers), monitor and control building hardware (door locks, sensors, relays), and interface with external systems (e.g., via MQTT for web dashboards). The suite includes a Windows desktop application for configuration and monitoring, and background services for real-time device communication.

C# (.NET 4.6.1) with WinForms for the admin UI.
MySQL database for storing users, cards, and device data.
Entity Framework 6 (planned integration) – currently using MySQL Connector/ADO.NET for data access.
Windows Service components for hardware communication (sockets, UDP broadcasts, MQTT messaging).
Integration of third-party libraries: MQTT (for IoT messaging), BouncyCastle (cryptography), EPPlus (Excel import/export), etc.

The core functionality (card management, device interfacing, etc.) is in place and was tested in a live environment with legacy hardware controllers. Some features (e.g., initial database setup, SIP integration, and video monitoring) are prototypes or pending completion. Given more time, I would improve this project by migrating to Entity Framework (to simplify database interactions) and adding a setup wizard to handle first-time database initialization. I would also refactor some modules for better separation of concerns (e.g., decouple the service logic from the UI).

Feel free to explore the code; notable sections include the custom MySQL connection pool in MCICommon and the DeviceServer service that manages device networks.

This project was developed as a prototype for a building security system in a 12 story building. It successfully controlled 30 doors in real-time.
