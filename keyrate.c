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
        }
        else
        {
            fprintf(stderr, "Failed to add program to startup.\n");
        }
        RegCloseKey(hKey);
    }
    else
    {
        fprintf(stderr, "Unable to open registry key.\n");
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
    if (!SystemParametersInfo(SPI_SETFILTERKEYS, 0, (LPVOID)&keys, 0))
    {
        fprintf(stderr, "System call failed.\nUnable to set keyrate.");
    }

    return 0;
}
