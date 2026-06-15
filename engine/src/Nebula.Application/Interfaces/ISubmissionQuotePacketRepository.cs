using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface ISubmissionQuotePacketRepository
{
    Task<SubmissionQuotePacket?> GetBySubmissionIdAsync(Guid submissionId, CancellationToken ct = default);
    Task AddAsync(SubmissionQuotePacket packet, CancellationToken ct = default);
    Task UpdateAsync(SubmissionQuotePacket packet, CancellationToken ct = default);
}
