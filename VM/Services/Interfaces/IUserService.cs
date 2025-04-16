using CMS.Dto;
using VM.Domains;
using VM.Dtos;

namespace VM.Services.Interfaces;

public interface IUserService
{
    Task<AppUser?> GetUserAsync(string userId);
    Task<AppUser> CreateUserAsync(string userId, string name, string imgPath = "");
    Task<double> GetBalanceAsync(string userId);
    Task<ResponseDto> ConfirmOrderPayment(string userId, double amount);
    Task<AppUserTransactionDto> GetTransactionsAsync(string userId);
    Task PostNewUserAsync(string userId);
    Task<SpendDto> GetLocalSpendAsync(string userId);
}
