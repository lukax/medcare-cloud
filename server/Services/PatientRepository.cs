using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Server.Core.Models;

namespace IdSvrHost.Services
{
    public class PagedListResult<T>
    {
        public IEnumerable<T> Result { get; set; }
        public long TotalCount { get; set; }
        public long TotalPages { get; set; }
    }

    public abstract class OperationResult 
    {
        public bool IsError { get; }
        public string Message { get; }

        public OperationResult(bool isError, string message)
        {
            IsError = isError;
            Message = message;
        }
    }
    public class SuccessOperationResult : OperationResult {
        public SuccessOperationResult(string message = "Ação concluída com sucesso."): base(false, message)
        {
        }
    }
    public class FailOperationResult : OperationResult {
        public FailOperationResult(string message = "Não foi possível completar ação."): base(true, message)
        {
        }
    }

    public class PatientRepository 
    {
        private const string PatientsCollectionName = "Patients";
        private readonly IMongoDatabase _db;
        private static bool _indexKeysCreated = false;
        private FilterDefinitionBuilder<Patient> FilterBuilder => Builders<Patient>.Filter;
        private SortDefinitionBuilder<Patient> SortBuilder => Builders<Patient>.Sort;
        private ProjectionDefinitionBuilder<Patient> ProjectionBuilder => Builders<Patient>.Projection;
        private IMongoCollection<Patient> Collection => _db.GetCollection<Patient>(PatientsCollectionName);
            
        public PatientRepository(IOptions<MongoDbRepositoryConfiguration> config)
        {
            var client = new MongoClient(config.Value.ConnectionString);
            _db = client.GetDatabase(config.Value.DatabaseName);
        }

        public async Task CreateIndexes()
        {
            if (_indexKeysCreated) return;
            _indexKeysCreated = true;
            await Collection.Indexes.CreateOneAsync(Builders<Patient>.IndexKeys.Combine(Builders<Patient>.IndexKeys.Ascending(x => x.UserId), Builders<Patient>.IndexKeys.Ascending(x => x.Id)));
            await Collection.Indexes.CreateOneAsync(Builders<Patient>.IndexKeys.Combine(Builders<Patient>.IndexKeys.Ascending(x => x.UserId), Builders<Patient>.IndexKeys.Descending(x => x.LastAppointment)));
        }
        
        public async Task<PagedListResult<PatientListViewModel>> GetAll(
            string userId, int pageSize = 50, int pageNumber = 1, string orderBy = "", string search = "")
        {
            var totalCount = await this.Count(userId); 
            var totalPages = (long) Math.Ceiling((double)totalCount / pageSize); 

            var filter = FilterBuilder.Eq(u => u.UserId, userId);
            if(!string.IsNullOrWhiteSpace(search)){
                filter = FilterBuilder.And(filter, 
                    FilterBuilder.Regex(u => u.Name, new BsonRegularExpression($@"/{search}/i")));
            }
            var sort = SortBuilder.Descending(x => x.LastAppointment);
            if(!string.IsNullOrWhiteSpace(orderBy)){
                sort = SortBuilder.Ascending(orderBy);
            }
            ProjectionDefinition<Patient, PatientListViewModel> projection = ProjectionBuilder
                                            .Include(x => x.Id)
                                            .Include(x => x.Name)
                                            .Include(x => x.Email)
                                            .Include(x => x.TaxIdNumber)
                                            .Include(x => x.MedicalInsurance)
                                            .Include(x => x.LastAppointment);
            return new PagedListResult<PatientListViewModel> {
                TotalCount = totalCount,
                TotalPages = totalPages,
                Result = await Collection
                    .Find(filter)
                    .Skip((pageNumber - 1) * pageSize)
                    .Limit(pageSize)
                    .Project(projection)
                    .Sort(sort)
                    .ToListAsync()
            };
        }

        public async Task<long> Count(string userId) 
        {
            var filter = FilterBuilder.Eq(u => u.UserId, userId);
            return await Collection.CountAsync(filter);
        }

        public async Task<Patient> FindOne(string userId, string patientId)
        {
            var filter = FilterBuilder.And(
                FilterBuilder.Eq(u => u.UserId, userId),
                FilterBuilder.Eq(u => u.Id, patientId)
            );
            return await Collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<OperationResult> DeleteOne(string userId, string patientId)
        {
            var filter = FilterBuilder.And(
                FilterBuilder.Eq(u => u.UserId, userId),
                FilterBuilder.Eq(u => u.Id, patientId)
            );
            var res = await Collection.DeleteOneAsync(filter);
            if(res.DeletedCount > 0){
                return new SuccessOperationResult();
            }
            return new FailOperationResult("Nenhum paciente encontrado");
        }

        public async Task<OperationResult> InsertOne(string userId, Patient patient)
        {
            patient.UserId = userId;
            var filter = FilterBuilder.Or(
                FilterBuilder.And(
                    FilterBuilder.Eq(u => u.UserId, patient.UserId),
                    FilterBuilder.Eq(u => u.Id, patient.Id)
                ),
                FilterBuilder.And(
                    FilterBuilder.Eq(u => u.UserId, patient.UserId),
                    FilterBuilder.Eq(u => u.TaxIdNumber, patient.TaxIdNumber)
                ));
            if(await Collection.CountAsync(filter) > 0)
            {
                return new FailOperationResult("Já existe um paciente com essas informações.");
            }
            else
            {
                await Collection.InsertOneAsync(patient);
                return new SuccessOperationResult();
            }
        }

        public async Task<OperationResult> InsertMany(string userId, List<Patient> patients)
        {
            patients.ForEach(x => x.UserId = userId);
            await Collection.InsertManyAsync(patients);
            return new SuccessOperationResult();
        }

        public async Task<OperationResult> UpdateOne(string userId, Patient patient)
        {
            patient.UserId = userId;
            var filter = FilterBuilder.And(
                    FilterBuilder.Eq(u => u.UserId, patient.UserId),
                    FilterBuilder.Eq(u => u.Id, patient.Id)
                );
            if(await Collection.CountAsync(filter) == 0)
            {
                return new FailOperationResult("Nenhum paciente encontrado.");
            }
            else
            {
                var result = await Collection.ReplaceOneAsync(filter, patient);
                return result.ModifiedCount > 0 ? new SuccessOperationResult() as OperationResult : new FailOperationResult("Nenhuma modificação") as OperationResult;
            }
        }
    }
}