<p align="center">
  <img src="https://llama.airforce/card.png" width="300" alt="Llama Airforce">
  <p align="center">🦙✈️ Airdropping knowledge bombs and providing air support about the DeFi ecosystem</p>

  <p align="center">
    <a><img alt="Software License" src="https://img.shields.io/badge/license-MIT-brightgreen.svg?style=flat-square"></a>
    <a href="https://github.com/Llama-Airforce/Llama-Airforce-Net/actions"><img alt="Build Status" src="https://github.com/Llama-Airforce/Llama-Airforce-Net/actions/workflows/dotnet.yml/badge.svg"></a>
  </p>
</p>

# Llama Airforce .NET

This repository contains the public .NET back-end of the [Llama Airforce](https://llama.airforce) website. It includes a cron runner hosted on a VPS and its analytical code. The primary goal of this repository is to be open and transparant about our methods, and to give people the opportunity to contribute.

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
| `Llama.Airforce.Cron`      | The VPS cron runner that updates CRV and FXN bribe rounds, dashboards, and the Convex flyer using the logic in `Llama.Airforce.Jobs`. |
| `Llama.Airforce.Jobs`      | This project contains the analytical logic used by `Llama.Airforce.Cron`. |
| `Llama.Airforce.Database`  | The gateway to our database, self-explanatory.                                                                                                                                                                                                                                                       |
| `Llama.Airforce.Domain`    | Domain-driven models and logic shared across all our projects.                                                                                                                                                                                                                                       |
| `Llama.Airforce.SeedWork`  | A base project containing re-usable base classes and extension methods for all our logic and domain entities. See [the Microsoft page](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/seedwork-domain-model-base-classes-interfaces).            |
