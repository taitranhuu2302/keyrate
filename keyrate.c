#include <windows.h>
#include <stdlib.h>
#include <stdio.h>

#define DEFAULT_DELAY 180
#define DEFAULT_REPEAT 8
#define APP_NAME "KeyRateApp"
#define REG_PATH "Software\\Microsoft\\Windows\\CurrentVersion\\Run"

BOOL parseDword(const char* in, DWORD* out)
{
    char* end;
    long result = strtol(in, &end, 10);
    BOOL success = (errno == 0 && end != in);
    if (success)
    {
        *out = result;
    }
    return success;
}

void AddToStartup()
{
    HKEY hKey;
    LONG result;
    char filePath[MAX_PATH];

    // Lấy đường dẫn đầy đủ của chương trình
    GetModuleFileName(NULL, filePath, MAX_PATH);

    // Mở khóa Run trong registry
    result = RegOpenKeyEx(HKEY_CURRENT_USER, REG_PATH, 0, KEY_WRITE, &hKey);
    if (result == ERROR_SUCCESS)
    {
        // Thêm giá trị cho chương trình vào khóa Run
        result = RegSetValueEx(hKey, APP_NAME, 0, REG_SZ, (BYTE*)filePath, strlen(filePath) + 1);
        if (result == ERROR_SUCCESS)
        {
            printf("Program added to startup.\n");
            MessageBox(NULL, "Program successfully added to startup.", "Success", MB_OK | MB_ICONINFORMATION);
        }
        else
        {
            fprintf(stderr, "Failed to add program to startup.\n");
            MessageBox(NULL, "Failed to add program to startup.", "Error", MB_OK | MB_ICONERROR);
        }
        RegCloseKey(hKey);
    }
    else
    {
        fprintf(stderr, "Unable to open registry key.\n");
        MessageBox(NULL, "Unable to open registry key.", "Error", MB_OK | MB_ICONERROR);
    }
}

int main(int argc, char* argv[])
{
    FILTERKEYS keys = { sizeof(FILTERKEYS) };

    // Thêm chương trình vào khởi động cùng hệ thống
    AddToStartup();

    if (argc == 3
        && parseDword(argv[1], &keys.iDelayMSec)
        && parseDword(argv[2], &keys.iRepeatMSec))
    {
        printf("Setting keyrate: delay: %d, rate: %d\n", (int)keys.iDelayMSec, (int)keys.iRepeatMSec);
        keys.dwFlags = FKF_FILTERKEYSON | FKF_AVAILABLE;
    }
    else if (argc == 1)
    {
        // Sử dụng giá trị mặc định nếu không có tham số
        keys.iDelayMSec = DEFAULT_DELAY;
        keys.iRepeatMSec = DEFAULT_REPEAT;
        keys.dwFlags = FKF_FILTERKEYSON | FKF_AVAILABLE;
        printf("No parameters given. Using default keyrate: delay: %d, rate: %d\n", DEFAULT_DELAY, DEFAULT_REPEAT);
    }
    else
    {
        puts("Usage: keyrate <delay ms> <repeat ms>\nCall with no parameters to use default values or disable.");
        return 0;
    }

    // Thiết lập giá trị keyrate
    if (SystemParametersInfo(SPI_SETFILTERKEYS, 0, (LPVOID)&keys, 0))
    {
        MessageBox(NULL, "Keyrate successfully set.", "Success", MB_OK | MB_ICONINFORMATION);
    }
    else
    {
        fprintf(stderr, "System call failed.\nUnable to set keyrate.");
        MessageBox(NULL, "Unable to set keyrate.", "Error", MB_OK | MB_ICONERROR);
    }

    return 0;
}
