using System.Collections.Generic;

namespace Llama.Airforce.Domain.Models;

public record EpochV2(
    int Round,
    List<BribeV2> Bribes);

public record BribeV2(
    string Gauge,
    string Token,
    string Amount,
    string MaxPerVote);
