using Microsoft.EntityFrameworkCore;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class SubmissionQuotePacketRepository(AppDbContext db) : ISubmissionQuotePacketRepository
{
    public async Task<SubmissionQuotePacket?> GetBySubmissionIdAsync(Guid submissionId, CancellationToken ct = default) =>
        await db.SubmissionQuotePackets
            .FirstOrDefaultAsync(packet => packet.SubmissionId == submissionId, ct);

    public async Task AddAsync(SubmissionQuotePacket packet, CancellationToken ct = default) =>
        await db.SubmissionQuotePackets.AddAsync(packet, ct);

    public Task UpdateAsync(SubmissionQuotePacket packet, CancellationToken ct = default) =>
        Task.CompletedTask;
}
