using MongoDB.Driver;
using System.Threading.Tasks;
using ResumeTemplateService.Application.Interfaces;
using ResumeTemplateService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ResumeTemplateService.Infrastructure.Repositories;

public class ResumeRepository : IResumeRepository
{
    private readonly IMongoCollection<ResumeProfile> _collection;
    private readonly ILogger<ResumeRepository> _logger;

    public ResumeRepository(IMongoDatabase database, string collectionName, ILogger<ResumeRepository> logger)
    {
        _logger = logger;
        _collection = database.GetCollection<ResumeProfile>(collectionName);
    }

    public async Task<ResumeProfile?> GetByIdAsync(string id)
    {
        try
        {
            _logger.LogInformation("Fetching resume with id: {ResumeId}", id);
            
            var filter = Builders<ResumeProfile>.Filter.Eq(r => r.Id, id);
            var resume = await _collection.Find(filter).FirstOrDefaultAsync();

            if (resume != null)
            {
                _logger.LogInformation("Resume found: {ResumeId}", id);
            }
            else
            {
                _logger.LogWarning("Resume not found: {ResumeId}", id);
            }

            return resume;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching resume with id: {ResumeId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string id)
    {
        try
        {
            _logger.LogDebug("Checking if resume exists: {ResumeId}", id);
            
            var filter = Builders<ResumeProfile>.Filter.Eq(r => r.Id, id);
            var count = await _collection.CountDocumentsAsync(filter);

            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if resume exists: {ResumeId}", id);
            throw;
        }
    }
}
