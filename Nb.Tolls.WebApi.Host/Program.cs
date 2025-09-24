using Microsoft.AspNetCore.Builder;
using Nb.Tolls.Application.Registrations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTollsApplication();
