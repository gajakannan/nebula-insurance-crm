# Architect Analysis

## Root Cause

`CommunicationService.CorrectOrRedactAsync` created a new `CommunicationCorrection` audit entity and added it through the loaded `communication.Corrections` navigation collection. In the local API runtime, EF generated an `UPDATE "CommunicationCorrections"` for the new audit row and raised `DbUpdateConcurrencyException` because no row existed.

## Fix Strategy

Stage the correction audit row explicitly through `ICommunicationRepository.AddCorrectionAsync`, with `CommunicationEventId` set before save. This preserves the existing aggregate update and timeline emission while making the audit row insertion unambiguous.

## Risk Assessment

Low to medium. The change is localized to F0021 correction/redaction persistence. The final E2E validates both correction and Admin redaction against the running API.
