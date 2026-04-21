using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;

namespace Nebula.Application.Services;

public class ReferenceDataService(IReferenceDataRepository refRepo)
{
    public async Task<IReadOnlyList<MgaDto>> GetMgasAsync(CancellationToken ct = default)
    {
        var mgas = await refRepo.GetMgasAsync(ct);
        return mgas.Select(m => new MgaDto(m.Id, m.Name, m.Status)).ToList();
    }

    public async Task<IReadOnlyList<ProgramDto>> GetProgramsAsync(CancellationToken ct = default)
    {
        var programs = await refRepo.GetProgramsAsync(ct);
        return programs.Select(p => new ProgramDto(p.Id, p.Name, p.MgaId)).ToList();
    }
}
