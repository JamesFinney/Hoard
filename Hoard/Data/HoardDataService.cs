using Hoard.Data;
using log4net;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Hoard.Data
{
    public class HoardDataService
    {
        static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static string _connectionString = "mongodb://localhost:27017";
        private static string _databaseName = "hoard";
        private static string _projectsCollectionName = "projects";
        private static string _notesCollectionName = "notes";
        private static string _entriesCollectionName = "entries";

        private static IMongoClient _client;
        private static IMongoDatabase _database;
        private static IMongoCollection<Project> _projectsCollection;
        private static IMongoCollection<Note> _notesCollection;
        private static IMongoCollection<Entry> _entriesCollection;


        static HoardDataService()
        {
            _client = new MongoClient(_connectionString);
            _database = _client.GetDatabase(_databaseName);
            _projectsCollection = _database.GetCollection<Project>(_projectsCollectionName);
            _notesCollection = _database.GetCollection<Note>(_notesCollectionName);
            _entriesCollection = _database.GetCollection<Entry>(_entriesCollectionName);
        }


        #region Project

        public async Task<bool> SaveProjectAsync(Project item)
        {
            try
            {
                var filter = Builders<Project>.Filter.Eq("_id", item.Id);
                await _projectsCollection.ReplaceOneAsync(filter, item, new UpdateOptions { IsUpsert = true });

                return true;
            }
            catch (Exception e)
            {
                Log.Error("Exception", e);
                return false;
            }
        }

        public async Task<List<Project>> GetProjectsAsync(string userId, bool isDeleted = false)
        {       
            try
            {
                var filter = Builders<Project>.Filter.Eq("userId", userId);
                var deletedFilter = Builders<Project>.Filter.Eq("isDeleted", isDeleted);

                var andFilter = Builders<Project>.Filter.And(filter, deletedFilter);

                var result = await _projectsCollection.FindAsync(andFilter);
                return await result.ToListAsync();
            }
            catch (Exception e)
            {
                Log.Error("Exception", e);
                return new List<Project>();
            }
        }

        public async Task<Project> GetProjectByIdAsync(string projectId, string userId)
        {
            try
            {
                var idFilter = Builders<Project>.Filter.Eq("_id", projectId);
                var userFilter = Builders<Project>.Filter.Eq("userId", userId);
                var filter = Builders<Project>.Filter.And(idFilter, userFilter);
                var result = await _projectsCollection.FindAsync(filter);
                return await result.FirstOrDefaultAsync();
            }
            catch (Exception e)
            {
                Log.Error("Exception", e);
                return null;
            }
        }

        public async Task<bool> DeleteProjectAsync(string projectId, string userId, bool isPermanent = false)
        {
            var session = _client.StartSession();
            try
            {
                var database = session.Client.GetDatabase(_databaseName);
                var notesCollection = database.GetCollection<Note>(_notesCollectionName);
                var entriesCollection = database.GetCollection<Entry>(_entriesCollectionName);
                var projectsCollection = database.GetCollection<Project>(_projectsCollectionName);

                session.StartTransaction();

                // delete notes
                {
                    var idFilter = Builders<Note>.Filter.Eq("projectId", projectId);
                    var userFilter = Builders<Note>.Filter.Eq("userId", userId);
                    var findFilter = Builders<Note>.Filter.And(idFilter, userFilter);
                    if (!isPermanent)
                    {
                        var update = Builders<Note>.Update.Set("isDeleted", true);
                        _ = await notesCollection.UpdateManyAsync(findFilter, update);
                    }
                    else
                    {
                        await notesCollection.DeleteManyAsync(findFilter);
                    }
                }

                // delete entries
                {
                    var idFilter = Builders<Entry>.Filter.Eq("projectId", projectId);
                    var userFilter = Builders<Entry>.Filter.Eq("userId", userId);
                    var findFilter = Builders<Entry>.Filter.And(idFilter, userFilter);
                    if (!isPermanent)
                    {
                        var update = Builders<Entry>.Update.Set("isDeleted", true);
                        _ = await entriesCollection.UpdateManyAsync(findFilter, update);
                    }
                    else
                    {
                        await entriesCollection.DeleteManyAsync(findFilter);
                    }
                }

                // delete
                {
                    var idFilter = Builders<Project>.Filter.Eq("_id", projectId);
                    var userFilter = Builders<Project>.Filter.Eq("userId", userId);
                    var findFilter = Builders<Project>.Filter.And(idFilter, userFilter);
                    if (!isPermanent)
                    {
                        var update = Builders<Project>.Update.Set("isDeleted", true);
                        _ = await projectsCollection.UpdateOneAsync(findFilter, update);
                    }
                    else
                    {
                        await _projectsCollection.DeleteOneAsync(findFilter);
                    }
                }

                await session.CommitTransactionAsync();

                return true;
            }
            catch (Exception e)
            {
                Log.Error("Exception", e);
                session.AbortTransaction();
                return false;
            }
        }

        public async Task<bool> RestoreProjectAsync(string projectId, string userId)
        {
            var session = _client.StartSession();
            try
            {
                var database = session.Client.GetDatabase(_databaseName);
                var notesCollection = database.GetCollection<Note>(_notesCollectionName);
                var entriesCollection = database.GetCollection<Entry>(_entriesCollectionName);
                var projectsCollection = database.GetCollection<Project>(_projectsCollectionName);

                session.StartTransaction();

                // delete notes
                var notesFilter = Builders<Note>.Filter.Eq("projectId", projectId);
                var userNotesFilter = Builders<Note>.Filter.Eq("userId", userId);
                var findNotesFilter = Builders<Note>.Filter.And(notesFilter, userNotesFilter);

                var noteUpdate = Builders<Note>.Update.Set("isDeleted", false);
                _ = await notesCollection.UpdateManyAsync(findNotesFilter, noteUpdate);

                // delete entries
                var entriesFilter = Builders<Entry>.Filter.Eq("projectId", projectId);
                var userEntriesFilter = Builders<Entry>.Filter.Eq("userId", userId);
                var findEntriesFilter = Builders<Entry>.Filter.And(entriesFilter, userEntriesFilter);

                var entryUpdate = Builders<Entry>.Update.Set("isDeleted", false);
                _ = await entriesCollection.UpdateManyAsync(findEntriesFilter, entryUpdate);

                // delete
                var projectFilter = Builders<Project>.Filter.Eq("_id", projectId);
                var userProjectFilter = Builders<Project>.Filter.Eq("userId", userId);
                var findProjectFilter = Builders<Project>.Filter.And(projectFilter, userProjectFilter);

                var projectUpdate = Builders<Project>.Update.Set("isDeleted", false);
                _ = await projectsCollection.UpdateOneAsync(findProjectFilter, projectUpdate);


                await session.CommitTransactionAsync();

                return true;
            }
            catch (Exception e)
            {
                Log.Error("Exception", e);
                session.AbortTransaction();
                return false;
            }
        }


        #endregion

        #region Entries

        public async Task<EntryQueryResult> SearchEntries(Query query, bool isDeleted = false)
        {
            try
            {
                FilterDefinition<Entry> filter = null;
                SortDefinition<Entry> sortDefinition = null;

                // FILTER
                var projectFilter = Builders<Entry>.Filter.Eq("projectId", query.ProjectId);
                var userFilter = Builders<Entry>.Filter.Eq("userId", query.UserId);
                var deletedFilter = Builders<Entry>.Filter.Eq("isDeleted", isDeleted);

                if (!string.IsNullOrEmpty(query.Search))
                {
                    var searchFilter = Builders<Entry>.Filter.Text(query.Search, "english");
                    filter = Builders<Entry>.Filter.And(projectFilter, userFilter, deletedFilter, searchFilter);
                }
                else
                {
                    filter = Builders<Entry>.Filter.And(projectFilter, userFilter, deletedFilter);
                }

                // SORT
                if(query.Sort == "score")
                {
                    sortDefinition = Builders<Entry>.Sort.MetaTextScore("Score");
                }
                else
                {
                    if (query.IsAscending)
                    {
                        sortDefinition = Builders<Entry>.Sort.Ascending(query.Sort);
                    }
                    else
                    {
                        sortDefinition = Builders<Entry>.Sort.Descending(query.Sort);
                    }
                }

                // PROJECTION (always project)
                var projectionDefinition = Builders<Entry>.Projection.MetaTextScore("Score");

                // QUERY
                var initialResult = _entriesCollection.Find(filter).Project<Entry>(projectionDefinition).Sort(sortDefinition);
                var total = initialResult.CountDocuments();
                var results = await initialResult.Skip(query.Skip).Limit(query.Take).ToListAsync();

                // RESULT
                return new EntryQueryResult
                {
                    Query = query,
                    Total = total,
                    Returned = results.Count,
                    Results = results
                };
            }
            catch (Exception e)
            {
                Log.Error("Exception", e);
                return null;
            }
        }

        public async Task<List<Entry>> GetEntriesByProjectId(string entryId, string userId)
        {
            try
            {
                var idFilter = Builders<Entry>.Filter.Eq("projectId", entryId);
                var userFilter = Builders<Entry>.Filter.Eq("userId", userId);
                var filter = Builders<Entry>.Filter.And(idFilter, userFilter);

                var result = await _entriesCollection.FindAsync(filter);
                return await result.ToListAsync();
            }
            catch (Exception e)
            {
                Log.Error("Exception", e);
                return null;
            }
        }

        public async Task<List<Entry>> GetDeletedEntriesByUser(string userId)
        {
            try
            {
                var userFilter = Builders<Entry>.Filter.Eq("userId", userId);
                var deletedFilter = Builders<Entry>.Filter.Eq("isDeleted", true);
                var filter = Builders<Entry>.Filter.And(userFilter, deletedFilter);

                var result = await _entriesCollection.FindAsync(filter);
                return await result.ToListAsync();
            }
            catch (Exception e)
            {
                Log.Error("Exception", e);
                return null;
            }
        }

        public async Task<long> CountEntriesByProjectId(string projectId, string userId, bool isDeleted = false)
        {
            try
            {
                var idFilter = Builders<Entry>.Filter.Eq("projectId", projectId);
                var userFilter = Builders<Entry>.Filter.Eq("userId", userId);
                var deletedFilter = Builders<Entry>.Filter.Eq("isDeleted", isDeleted);
                var filter = Builders<Entry>.Filter.And(idFilter, userFilter, deletedFilter);

                var result = await _entriesCollection.Find(filter).CountDocumentsAsync();
                return result;
            }
            catch (Exception e)
            {
                Log.Error("Exception", e);
                return -1;
            }
        }

        public async Task<Entry> GetEntriesById(string entryId, string userId)
        {
            try
            {
                var idFilter = Builders<Entry>.Filter.Eq("_id", entryId);
                var userFilter = Builders<Entry>.Filter.Eq("userId", userId);
                var filter = Builders<Entry>.Filter.And(idFilter, userFilter);

                var result = await _entriesCollection.FindAsync(filter);
                return await result.FirstOrDefaultAsync();
            }
            catch (Exception e)
            {
                Log.Error("Exception", e);
                return null;
            }
        }

        public async Task<bool> SaveEntryAsync(Entry item)
        {
            try
            {
                var filter = Builders<Entry>.Filter.Eq("_id", item.Id);
                await _entriesCollection.ReplaceOneAsync(filter, item, new UpdateOptions { IsUpsert = true });

                return true;
            }
            catch (Exception e)
            {
                Log.Error("Exception", e);
                return false;
            }
        }

        public async Task DeleteEntryAsync(string entryId, string userId, bool isPermanent = false)
        {
            try
            {
                var idFilter = Builders<Entry>.Filter.Eq("_id", entryId);
                var userFilter = Builders<Entry>.Filter.Eq("userId", userId);
                var filter = Builders<Entry>.Filter.And(idFilter, userFilter);

                if (!isPermanent)
                {
                    var update = Builders<Entry>.Update.Set("isDeleted", true);
                    _ = await _entriesCollection.UpdateOneAsync(filter, update);
                }
                else
                {
                    await _entriesCollection.DeleteOneAsync(filter);
                }
            }
            catch (Exception e)
            {
                Log.Error("Exception", e);
            }
        }

        #endregion

        #region Notes

        public async Task<NoteQueryResult> SearchNotes(Query query, bool isDeleted = false)
        {
            try
            {
                FilterDefinition<Note> filter = null;
                SortDefinition<Note> sortDefinition = null;

                // FILTER
                var projectFilter = Builders<Note>.Filter.Eq("projectId", query.ProjectId);
                var userFilter = Builders<Note>.Filter.Eq("userId", query.UserId);
                var deletedFilter = Builders<Note>.Filter.Eq("isDeleted", isDeleted);

                if (!string.IsNullOrEmpty(query.Search))
                {
                    var searchFilter = Builders<Note>.Filter.Text(query.Search, "english");
                    filter = Builders<Note>.Filter.And(projectFilter, userFilter, deletedFilter, searchFilter);
                }
                else
                {
                    filter = Builders<Note>.Filter.And(projectFilter, userFilter, deletedFilter);
                }

                // SORT
                if (query.Sort == "score")
                {
                    sortDefinition = Builders<Note>.Sort.MetaTextScore("Score");
                }
                else
                {
                    if (query.IsAscending)
                    {
                        sortDefinition = Builders<Note>.Sort.Ascending(query.Sort);
                    }
                    else
                    {
                        sortDefinition = Builders<Note>.Sort.Descending(query.Sort);
                    }
                }

                // PROJECTION (always project)
                var projectionDefinition = Builders<Note>.Projection.MetaTextScore("Score");

                // QUERY
                var initialResult = _notesCollection.Find(filter).Project<Note>(projectionDefinition).Sort(sortDefinition);
                var total = initialResult.CountDocuments();
                var results = await initialResult.Skip(query.Skip).Limit(query.Take).ToListAsync();

                // RESULT
                return new NoteQueryResult
                {
                    Query = query,
                    Total = total,
                    Returned = results.Count,
                    Results = results
                };
            }
            catch (Exception e)
            {
                Log.Error("Exception", e);
                return null;
            }
        }

        public async Task<List<Note>> GetNotesByProjectId(string projectId, string userId)
        {
            try
            {
                var projectFilter = Builders<Note>.Filter.Eq("projectId", projectId);
                var userFilter = Builders<Note>.Filter.Eq("userId", projectId);
                var filter = Builders<Note>.Filter.And(projectFilter, userFilter);

                var result = await _notesCollection.FindAsync(filter);
                return await result.ToListAsync();
            }
            catch (Exception e)
            {
                Log.Error("Exception", e);
                return null;
            }
        }

        public async Task<Note> GetNoteById(string noteId, string userId)
        {
            try
            {
                var idFilter = Builders<Note>.Filter.Eq("_id", noteId);
                var userFilter = Builders<Note>.Filter.Eq("userId", userId);
                var filter = Builders<Note>.Filter.And(idFilter, userFilter);

                var result = await _notesCollection.FindAsync(filter);
                return await result.FirstOrDefaultAsync();
            }
            catch (Exception e)
            {
                Log.Error("Exception", e);
                return null;
            }
        }

        public async Task<List<Note>> GetDeletedNotesByUser(string userId)
        {
            try
            {
                var userFilter = Builders<Note>.Filter.Eq("userId", userId);
                var deletedFilter = Builders<Note>.Filter.Eq("isDeleted", true);
                var filter = Builders<Note>.Filter.And(userFilter, deletedFilter);

                var result = await _notesCollection.FindAsync(filter);
                return await result.ToListAsync();
            }
            catch (Exception e)
            {
                Log.Error("Exception", e);
                return null;
            }
        }

        public async Task<bool> SaveNoteAsync(Note item)
        {
            try
            {
                var filter = Builders<Note>.Filter.Eq("_id", item.Id);
                await _notesCollection.ReplaceOneAsync(filter, item, new UpdateOptions { IsUpsert = true });

                return true;
            }
            catch (Exception e)
            {
                Log.Error("Exception", e);
                return false;
            }
        }

        public async Task<long> CountNotesByProjectId(string projectId, string userId, bool isDeleted = false)
        {
            try
            {
                var idFilter = Builders<Note>.Filter.Eq("projectId", projectId);
                var userFilter = Builders<Note>.Filter.Eq("userId", userId);
                var deletedFilter = Builders<Note>.Filter.Eq("isDeleted", isDeleted);
                var filter = Builders<Note>.Filter.And(idFilter, userFilter, deletedFilter);

                var result = await _notesCollection.Find(filter).CountDocumentsAsync();
                return result;
            }
            catch (Exception e)
            {
                Log.Error("Exception", e);
                return -1;
            }
        }

        public async Task DeleteNoteAsync(string noteId, string userId, bool isPermanent = false)
        {
            try
            {
                var idFilter = Builders<Note>.Filter.Eq("_id", noteId);
                var userFilter = Builders<Note>.Filter.Eq("userId", userId);
                var filter = Builders<Note>.Filter.And(idFilter, userFilter);

                if (!isPermanent)
                {
                    var update = Builders<Note>.Update.Set("isDeleted", true);
                    _ = await _notesCollection.UpdateOneAsync(filter, update);
                }
                else
                {
                    await _notesCollection.DeleteOneAsync(filter);
                }
            }
            catch (Exception e)
            {
                Log.Error("Exception", e);
            }
        }


        #endregion

    }
}
