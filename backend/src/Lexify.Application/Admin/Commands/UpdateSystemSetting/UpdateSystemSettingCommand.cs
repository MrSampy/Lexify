using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Admin.Commands.UpdateSystemSetting;

public sealed record UpdateSystemSettingCommand(string Key, string Value) : IRequest<Result>;
