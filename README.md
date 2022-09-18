<p align="center">
  <img src="https://llama.airforce/card.png" width="300" alt="Llama Airforce">
  <p align="center">🦙✈️ Airdropping knowledge bombs and providing air support about the DeFi ecosystem</p>

  <p align="center">
    <a><img alt="Software License" src="https://img.shields.io/badge/license-MIT-brightgreen.svg?style=flat-square"></a>
    <a href="https://github.com/Llama-Airforce/Llama-Airforce-Net/actions"><img alt="Build Status" src="https://github.com/Llama-Airforce/Llama-Airforce-Net/actions/workflows/dotnet.yml/badge.svg"></a>
  </p>
</p>

# Llama Airforce .NET

This repository contains the public .NET back-end of the [Llama Airforce](https://llama.airforce) website. It includes an API server, Azure Functions with its corresponding analytical code and more. The primary goal of this repository is to be open and transparant about our methods, and to give people the opportunity to contribute.

The back-end makes use of the following technologies, frameworks and libraries:

- [.NET 6](https://dotnet.microsoft.com/en-us/)
- [LanguageExt](https://github.com/louthy/language-ext)
- [Nethereum](https://nethereum.com/)

Although all the code is written in C#, it makes heavy use of a functional programming style using the great [LanguageExt](https://github.com/louthy/language-ext) library.

## Installation

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build --verbosity normal
```

## Projects

| Project                    | Description                                                                                                                                                                                                                                                                                          |
| -------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Llama.Airforce.API`       | The primary API server found at https://api.llama.airforce for use in conjunction with our front-end website.                                                                                                                                                                                        |
| `Llama.Airforce.Functions` | The various worker jobs we have running that keep our database up-to-date with the latest data and analytics, including the various bribe rounds. This project mainly contains the Azure Functions themselves and acts as a facade of the actual logic, which can be found in `Llama.Airforce.Jobs`. |
| `Llama.Airforce.Jobs`      | This project contains the analytical logic used by the Azure Functions in `Llama.Airforce.Functions`                                                                                                                                                                                                 |
| `Llama.Airforce.Database`  | The gateway to our database, self-explanatory.                                                                                                                                                                                                                                                       |
| `Llama.Airforce.Domain`    | Domain-driven models and logic shared across all our projects.                                                                                                                                                                                                                                       |
| `Llama.Airforce.SeedWork`  | A base project containing re-usable base classes and extension methods for all our logic and domain entities. See [the Microsoft page](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/seedwork-domain-model-base-classes-interfaces).            |
