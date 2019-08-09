# Hoard
ASP.NET Core Server-side Blazor application for storing, editing and searching your information.


# Features
* User registration and login
* HTTPS enabled
* Create, edit and delete:
  * Projects
  * Notes
  * Entries
* Add tags and images to entries
* Independantly search for notes and entries
  * Sort results by relevance, created and edited (ascending or descending)
  * Result pagination with selectable page size
* Recycle bin for physically deleting items
* Restore items from the recycle bin


# Stack
* ASP.NET Core 3 preview server-side application with SSL
* Blazor + Boostrap front-end
* Core Identity authorisation and authentication
* MS SQL Server identity store (see future tasks)
* MongoDB data storage with transactions (4.0.10)
* Developed using Visual Studio 2019 Preview

# Use

This assumes you'll be running the application and database locally through Visual Studio:

1. Install the latest MongoDB: <https://www.mongodb.com/download-center/community>
2. Open the MongoDB shell (for me on Windows it's C:\Program Files\MongoDB\Server\4.0\bin\mongo.exe)
3. Run the following commands to add the required indexes:

```javascript
use hoard
db.entries.createIndex({ projectId: 1, userId: 1, textContent: "text", source: "text", tags: "text"})
db.notes.createIndex({ projectId: 1, userId: 1, textContent: "text"})
```

4. Clone this repository
5. Open the solution in Visual Studio and run (either in IIS Express of as a standalone console application)


# Concepts

Hoard has 3 simple data types: Projects, Notes and Entries.

**Project**
* A project consists of a name and a brief description. All notes and entries are logically grouped together within a single project. 

**Note**
* A note matches the current notion of a note - it is a single block of text which can contain, for example, a to do list, or general thoughts about the project or topic your gathering information for.

**Entry**
* An entry is conceptually more granular than a note
* A note can contain text, tags, a source, and an image.
  * Tags are whitespace seperated (no hastags required)
  * Source refers to the Source of the text or image provided, such as the URL or the Author.


# Motivation

I was finding Notes and Word documents too linear, flat and 'monilithic' for some of the information I needed to store.

I wanted a simple application where I could store the same information but in smaller chunks and then use tags or the content itself to relate/link the information together. With a simple search function I could then locate that information when needed.


# Future tasks
* Move authorisation from MS SQL to MongoDB
* Ability to move notes/entries between projects
* Duplication of notes/entries
* Exporting of selected notes
* Search for notes and entries across projects
* Transfer/copy projects/notes/entries to another user


# Known issues/improvements
* Struggles with adding of images above a certain size. This causes the SignalR connection to disconnect
* Change view for small devices - a bit cluttered, e.g. the search bar
* Items are currently stored against the Identity.Name which is the user's email address - this should use something like a GUID to annonymise the owner.
* Still need to wrap some pages in authorisation views
* No data validation - at present you can add empty entries
* MongoDB connection currently hardcoded to localhost - move to config



