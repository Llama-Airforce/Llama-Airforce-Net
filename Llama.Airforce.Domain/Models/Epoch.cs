using System.Collections.Generic;

namespace Llama.Airforce.Domain.Models;

public record Epoch(
    string SnapshotId,
    long Deadline,
    List<Bribe> Bribes);

public record EpochV2(
    int Round,
    List<BribeV2> Bribes);

public record Bribe(
    int Choice,
    string Token,
    string Amount);

public record BribeV2(
    string Gauge,
    string Token,
    string Amount,
    string MaxPerVote);