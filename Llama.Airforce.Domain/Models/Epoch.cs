using System.Collections.Generic;

namespace Llama.Airforce.Domain.Models;

public record Epoch(
    string SnapshotId,
    long Deadline,
    List<Bribe> Bribes);

public record Bribe(
    int Choice,
    string Token,
    string Amount);