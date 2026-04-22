//소켓 생성, Accept, Receive, Send와 같은 통신에 필수적으로 필요한 함수, 구조체등이 정의되어있다.
#include <winsock2.h>
//Tcp/Ip와 관련된 기능들을 제공하며 사실상 winsock2 헤더파일과 관련된 편의성 기능들을 제공한다.
#include <ws2tcpip.h>
//Microsoft 전용 확장 window socket에 관한 함수들이 안에 들어있다.
#include <mswsock.h>
#include <iostream>
#include <vector>
//키-값으로 저장되는 이진트리 기반의 컨테이너.
#include <map>
#include <queue>
#include <stack>
#include <windows.h>
#include <conio.h>
//C언어 스타일의 문자열 조작
#include <cstring>
#include <chrono>
//mutex : Mutual Exclusion의 약자로 상호 배제라는 뜻을 가지고 있는데 스레드 공유 자원에 한 스레드만 접근하도록 제한하는 기법을 말한다. (Lock)
#include <mutex>
#include <thread>
#include <atomic>
#include <iomanip>

#pragma comment(lib, "Ws2_32.lib")
#pragma comment(lib, "Mswsock.lib")

// NetBase와 GGM_MMO_CPP include 추가 (충돌 방지를 위해, 상수 정의를 위해 먼저)
#include "NetBase.h"
#include "GGM_MMO_CPP.h"

using namespace NetBase;
using namespace GGM_MMO_CPP;

// 템플릿 기반 메모리 풀
template<typename T>
class MemoryPool {
private:
    std::stack<T*> m_pool;
    CRITICAL_SECTION m_poolCS;
    size_t m_poolSize;            // 풀에 있는 객체 수 (대기중)
    size_t m_maxPoolSize;         // 최대 풀 크기
    size_t m_totalAllocated;      // 총 할당된 객체 수
public:
    MemoryPool(size_t initialSize = 100, size_t maxSize = 1000)
        : m_poolSize(0), m_maxPoolSize(maxSize), m_totalAllocated(0) {
        InitializeCriticalSection(&m_poolCS);
        // 초기 메모리 풀 생성
        for (size_t i = 0; i < initialSize; ++i) {
            T* obj = new T();
            m_pool.push(obj);
            m_poolSize++;
            m_totalAllocated++;
        }
    }

    ~MemoryPool() {
        EnterCriticalSection(&m_poolCS);
        while (!m_pool.empty()) {
            delete m_pool.top();
            m_pool.pop();
        }
        LeaveCriticalSection(&m_poolCS);
        DeleteCriticalSection(&m_poolCS);
    }
    // 객체 할당
    T* Allocate() {
        EnterCriticalSection(&m_poolCS);

        T* obj = nullptr;
        if (m_pool.empty()) {
            // 풀이 비어있으면 새로 생성
            m_totalAllocated++;
            LeaveCriticalSection(&m_poolCS);
            return new T();
        }

        obj = m_pool.top();
        m_pool.pop();
        m_poolSize--;

        LeaveCriticalSection(&m_poolCS);

        // 객체 초기화 (생성자 재호출)
        new (obj) T();
        return obj;
    }

    // 객체 반환
    void Deallocate(T* obj) {
        if (!obj) return;

        // 소멸자 호출하여 정리
        obj->~T();

        EnterCriticalSection(&m_poolCS);

        // 풀 크기가 최대치에 도달했으면 삭제
        if (m_poolSize >= m_maxPoolSize) {
            m_totalAllocated--;
            LeaveCriticalSection(&m_poolCS);
            delete obj;
            return;
        }
        // 풀에 반환
        m_pool.push(obj);
        m_poolSize++;

        LeaveCriticalSection(&m_poolCS);
    }

    // 현재 풀 크기 반환
    size_t GetPoolSize() const {
        EnterCriticalSection(const_cast<CRITICAL_SECTION*>(&m_poolCS));
        size_t size = m_poolSize;
        LeaveCriticalSection(const_cast<CRITICAL_SECTION*>(&m_poolCS));
        return size;
    }

    // 사용 중인 객체 수 반환
    size_t GetInUseCount() const {
        EnterCriticalSection(const_cast<CRITICAL_SECTION*>(&m_poolCS));
        size_t inUse = m_totalAllocated - m_poolSize;
        LeaveCriticalSection(const_cast<CRITICAL_SECTION*>(&m_poolCS));
        return inUse;
    }

    // 총 할당된 객체 수 반환
    size_t GetTotalAllocated() const {
        EnterCriticalSection(const_cast<CRITICAL_SECTION*>(&m_poolCS));
        size_t total = m_totalAllocated;
        LeaveCriticalSection(const_cast<CRITICAL_SECTION*>(&m_poolCS));
        return total;
    }

    // 풀 통계 출력
    void PrintStats() const {
        EnterCriticalSection(const_cast<CRITICAL_SECTION*>(&m_poolCS));
        std::cout << "MemoryPool<" << typeid(T).name() << "> - Pool Size: "
            << m_poolSize << "/" << m_maxPoolSize << std::endl;
        LeaveCriticalSection(const_cast<CRITICAL_SECTION*>(&m_poolCS));
    }
};

#define PACKET_HEADER_SIZE sizeof(PacketHeader)
#define MAX_PACKET_SIZE 4096
#define MAX_MESSAGE_SIZE (MAX_PACKET_SIZE - PACKET_HEADER_SIZE - 1) // null terminator 고려
#define MAX_PACKETS_PER_CALL 10 // 한 번에 처리할 최대 패킷 수

// AcceptEx용 주소 정보 버퍼 크기
#define ACCEPT_ADDRESS_LENGTH (sizeof(sockaddr_in) + 16)
#define ACCEPT_BUFFER_SIZE (ACCEPT_ADDRESS_LENGTH * 2)

// I/O 작업 타입
enum IOType {
    IO_ACCEPT,
    IO_RECV,
    IO_SEND,
    IO_DISCONNECT // DisconnectEx용 추가
};

// IOCP에서 사용할 오버랩 구조체
struct IOContext {
    OVERLAPPED overlapped;   // 반드시 첫 번째 멤버여야 함
    IOType ioType;           // I/O 작업 타입
    SOCKET socket;           // 해당 소켓
    WSABUF wsaBuf;           // 버퍼 정보
    char buffer[MAX_PACKET_SIZE]; // 실제 데이터 버퍼
    int clientId;            // 클라이언트 ID

    // AcceptEx 전용 필드들
    SOCKET acceptSocket;     // AcceptEx로 생성되는 새 소켓
    char acceptBuffer[ACCEPT_BUFFER_SIZE]; // AcceptEx 주소 정보 버퍼
};

// 패킷 조립을 위한 버퍼 구조체
struct PacketBuffer {
    char data[MAX_PACKET_SIZE];
    int currentSize;         // 현재 받은 데이터 크기
    int expectedSize;        // 예상되는 전체 패킷 크기
    bool headerReceived;     // 헤더 수신 완료 여부

    PacketBuffer() : currentSize(0), expectedSize(0), headerReceived(false) {}

    void Reset() {
        expectedSize = 0;
        headerReceived = false;
        currentSize = 0;
    }
};

// 송신 패킷 누적을 위한 버퍼 구조체 (PacketBuffer와 유사)
struct SendPacketBuffer {
    char data[MAX_PACKET_SIZE * 10];  // 여러 패킷 누적 가능 (4096 * 10 = 40960 바이트)
    int currentSize;                  // 현재 누적된 데이터 크기

    SendPacketBuffer() : currentSize(0) {}

    void Reset() {
        currentSize = 0;
    }
};

// 클라이언트 정보 구조체
struct ClientInfo {
    SOCKET socket;
    int id;
    IOContext recvContext;  // 수신용 컨텍스트
    bool isConnected;
    PacketBuffer packetBuffer; // 패킷 조립용 버퍼

    // 송신 패킷 누적 버퍼 (SendPacketBuffer 사용)
    SendPacketBuffer sendBuffer;
};

// AcceptEx 함수 포인터 타입 정의
typedef BOOL(WINAPI* LPFN_ACCEPTEX)(
    SOCKET sListenSocket,
    SOCKET sAcceptSocket,
    PVOID lpOutputBuffer,
    DWORD dwReceiveDataLength,
    DWORD dwLocalAddressLength,
    DWORD dwRemoteAddressLength,
    LPDWORD lpdwBytesReceived,
    LPOVERLAPPED lpOverlapped
    );

typedef VOID(WINAPI* LPFN_GETACCEPTEXSOCKADDRS)(
    PVOID lpOutputBuffer,
    DWORD dwReceiveDataLength,
    DWORD dwLocalAddressLength,
    DWORD dwRemoteAddressLength,
    struct sockaddr** LocalSockaddr,
    LPINT LocalSockaddrLength,
    struct sockaddr** RemoteSockaddr,
    LPINT RemoteSockaddrLength
    );

// DisconnectEx 함수 포인터 타입 정의
typedef BOOL(WINAPI* LPFN_DISCONNECTEX)(
    SOCKET s,
    LPOVERLAPPED lpOverlapped,
    DWORD dwFlags,
    DWORD reserved
    );

bool PostProcessPacket(SOCKET socket, int id, char* packetData, int packetSize);
bool PostProcessNew(SOCKET socket, int id, ClientInfo* newClient);
bool PostProcessRemove(SOCKET socket, int id);

struct SendBuffer;
struct SendIOContext;
// 워커 태스크 타입
enum WorkerTaskType {
    TASK_PROCESS_PACKET,
    TASK_PROCESS_NEW_CLIENT,
    TASK_PROCESS_REMOVE_CLIENT,
    TASK_FLUSH_SEND_BUFFERS // 0.1초 간격 플러시 추가
};

// Sender 태스크 타입
enum SenderTaskType {
    SEND_BROADCAST,
    SEND_SINGLE,
    SEND_NEW,
    SEND_REMOVE
};

// 프로세스 태스크 구조체
struct ProcessTask {
    WorkerTaskType type;
    SOCKET socket;
    int id;
    char packetData[MAX_PACKET_SIZE];
    int packetSize;
    ClientInfo* clientInfo;
};

// Sender 태스크 구조체
struct SendTask {
    SenderTaskType type;
    SOCKET socket;
    SOCKET excludeSocket;
    int id;
    SendBuffer* sendBuffer;
    int packetSize;
    ClientInfo* clientInfo;
};

// 메모리 풀 전역 인스턴스들
MemoryPool<SendBuffer> g_sendBufferPool(10000, 100000);       // SendBuffer용 풀
MemoryPool<SendIOContext> g_sendIOContextPool(10000, 100000); // SendIOContext용 풀
MemoryPool<IOContext> g_ioContextPool(10000, 100000);         // IOContext용 풀
MemoryPool<ClientInfo> g_clientInfoPool(10000, 100000);       // ClientInfo용 풀
MemoryPool<ProcessTask> g_processTaskPool(10000, 100000);     // ProcessTask용 풀
MemoryPool<SendTask> g_sendTaskPool(1000, 10000);             // SendTask용 풀

// 전역 변수들
HANDLE g_hCompletionPort = NULL;                // 완료 포트 핸들
HANDLE g_hProcessCompletionPort = NULL;         // 프로세스 완료 포트 핸들
SOCKET g_listenSocket = INVALID_SOCKET;         // 리슨 소켓
std::map<SOCKET, ClientInfo*> g_clients;        // 클라이언트 맵
int g_clientCounter = 0;                        // 클라이언트 번호 카운터
bool g_serverRunning = true;                    // 서버 실행 상태
std::vector<HANDLE> g_senderCompletionPorts;    //Sender 완료 포트들

// AcceptEx 관련 전역 변수들
LPFN_ACCEPTEX g_lpfnAcceptEx = NULL;
LPFN_GETACCEPTEXSOCKADDRS g_lpfnGetAcceptExSockAddrs = NULL;
LPFN_DISCONNECTEX g_lpfnDisconnectEx = NULL;    // DisconnectEx 함수 포인터
std::vector<IOContext*> g_acceptContexts;       // AcceptEx용 컨텍스트들
const int g_acceptContextCount = 1000;          // 미리 준비할 AcceptEx 컨텍스트 수

// 통계 관련 구조체
struct PacketStats {
    long long totalProcessingTime = 0;
    long long packetCount = 0;
};

struct PacketCountData {
    std::chrono::steady_clock::time_point timestamp;
    int count;
};

// 통계 전역 변수
std::mutex g_packetStatsLock;
std::map<PacketType, PacketStats> g_packetStats;
std::map<PacketType, std::queue<PacketCountData>> g_packetCounts;

std::atomic<long long> g_totalBytesSent{ 0 };
std::atomic<long long> g_totalBytesReceived{ 0 };
std::atomic<long long> g_totalPacketsSent{ 0 };
std::atomic<long long> g_totalPacketsReceived{ 0 };

long long g_lastBytesSent = 0;
long long g_lastBytesReceived = 0;
std::chrono::steady_clock::time_point g_lastStatsTime;

// 타이머 관련 변수
std::thread g_updateTimerThread;
std::thread g_statsTimerThread;
bool g_timersRunning = true;
const std::chrono::minutes g_statsWindow{ 5 }; // 5분 윈도우

// 패킷 타입 유효성 검사
bool IsValidPacketType(PacketType type) {
    return (type >= PacketType::None && type <= PacketType::Max);
}

// 통계 업데이트 함수
void UpdatePacketStats(PacketType packetType, long long processingTime) {
    std::lock_guard<std::mutex> lock(g_packetStatsLock);
    if (g_packetStats.find(packetType) == g_packetStats.end()) {
        g_packetStats.emplace(packetType, PacketStats{});
    }

    g_packetStats[packetType].totalProcessingTime += processingTime;
    g_packetStats[packetType].packetCount++;
}

// 패킷 타입을 문자열로 변환
const char* PacketTypeToString(PacketType type) {
    switch (type) {
    case PacketType::None: return "CHAT_MESSAGE";
    case PacketType::ChatReq: return "SYSTEM_MESSAGE";
    case PacketType::ChatAck: return "CLIENT_JOIN";
    case PacketType::Max: return "CLIENT_LEAVE";
    default: return "UNKNOWN";
    }
}

// Update 이벤트 처리 (현재는 빈 구현)
void PostUpdateEvent() {
    // 실제 게임 로직 업데이트가 필요한 경우 여기에 구현
    // 예: 게임 오브젝트 업데이트, 물리 시뮬레이션 등

    // 추가: 0.1초 간격으로 플러시 이벤트 전송
    ProcessTask* task = g_processTaskPool.Allocate();
    task->type = TASK_FLUSH_SEND_BUFFERS;
    BOOL result = PostQueuedCompletionStatus(g_hProcessCompletionPort, 0, (ULONG_PTR)task, NULL);
    if (!result) {
        std::cout << "PostFlushEvent 실패: " << GetLastError() << "\n";
        g_processTaskPool.Deallocate(task);
    }
}

// 업데이트 타이머 스레드 함수
void UpdateTimerThread() {
    while (g_timersRunning) {
        PostUpdateEvent();
        std::this_thread::sleep_for(std::chrono::milliseconds(100)); // 100ms마다 실행
    }
}

// 통계 타이머 스레드 함수
void StatsTimerThread() {
    g_lastStatsTime = std::chrono::steady_clock::now();

    while (g_timersRunning) {
        std::this_thread::sleep_for(std::chrono::seconds(30)); // 30초마다 실행

        auto now = std::chrono::steady_clock::now();
        auto elapsed = std::chrono::duration_cast<std::chrono::milliseconds>(now - g_lastStatsTime).count() / 1000.0;

        long long bytesSentSinceLastCheck = g_totalBytesSent - g_lastBytesSent;
        long long bytesReceivedSinceLastCheck = g_totalBytesReceived - g_lastBytesReceived;

        double bytesSentPerSecond = bytesSentSinceLastCheck / elapsed;
        double bytesReceivedPerSecond = bytesReceivedSinceLastCheck / elapsed;

        // 현재 시간 출력
        auto timeT = std::chrono::system_clock::to_time_t(std::chrono::system_clock::now());
        struct tm timeStruct;
        localtime_s(&timeStruct, &timeT);

        std::cout << "\n[" << std::put_time(&timeStruct, "%Y-%m-%d %H:%M:%S") << "] 통계:\n";
        std::cout << "  누적 전송 패킷: " << g_totalPacketsSent << ", 누적 수신 패킷: " << g_totalPacketsReceived << "\n";
        std::cout << "  누적 전송 바이트: " << g_totalBytesSent << ", 누적 수신 바이트: " << g_totalBytesReceived << "\n";
        std::cout << "  초당 전송 바이트: " << std::fixed << std::setprecision(2) << bytesSentPerSecond << " B/s, ";
        std::cout << "초당 수신 바이트: " << bytesReceivedPerSecond << " B/s\n";

        std::cout << "패킷 처리 시간 통계:\n";
        {
            std::lock_guard<std::mutex> lock(g_packetStatsLock);
            for (const auto& stat : g_packetStats) {
                if (stat.second.packetCount > 0) {
                    double averageProcessingTime = (double)stat.second.totalProcessingTime / stat.second.packetCount;
                    std::cout << "  " << PacketTypeToString(stat.first) << ": 평균 "
                        << std::fixed << std::setprecision(2) << averageProcessingTime
                        << "ms (총 " << stat.second.packetCount << "개)\n";
                }
            }
        }

        std::cout << "최근 5분 동안의 각 패킷의 초당 Recv 카운트:\n";
        {
            std::lock_guard<std::mutex> lock(g_packetStatsLock);
            for (const auto& stat : g_packetStats) {
                PacketType packetType = stat.first;
                int newCount = (int)stat.second.packetCount;

                if (g_packetCounts.find(packetType) == g_packetCounts.end()) {
                    g_packetCounts[packetType] = std::queue<PacketCountData>();
                }

                g_packetCounts[packetType].push({ now, newCount });

                // 5분이 지난 데이터 제거
                while (!g_packetCounts[packetType].empty() &&
                    now - g_packetCounts[packetType].front().timestamp > g_statsWindow) {
                    g_packetCounts[packetType].pop();
                }

                if (g_packetCounts[packetType].size() > 1) {
                    const auto& oldest = g_packetCounts[packetType].front();
                    int countDiff = newCount - oldest.count;
                    auto timeDiff = std::chrono::duration_cast<std::chrono::milliseconds>(now - oldest.timestamp).count() / 1000.0;
                    double packetPerSecond = countDiff / timeDiff;

                    std::cout << "  " << PacketTypeToString(packetType) << ": "
                        << std::fixed << std::setprecision(2) << packetPerSecond << " packets/s\n";
                }
                else {
                    std::cout << "  " << PacketTypeToString(packetType) << ": 충분한 데이터가 없습니다.\n";
                }
            }
        }

        // 메모리 풀 통계 출력
        std::cout << "메모리 풀 통계:\n";
        std::cout << "  SendBuffer 풀: " << g_sendBufferPool.GetInUseCount() << "/"
            << g_sendBufferPool.GetTotalAllocated() << " 사용중\n";
        std::cout << "  SendIOContext 풀: " << g_sendIOContextPool.GetInUseCount() << "/"
            << g_sendIOContextPool.GetTotalAllocated() << " 사용중\n";
        std::cout << "  IOContext 풀: " << g_ioContextPool.GetInUseCount() << "/"
            << g_ioContextPool.GetTotalAllocated() << " 사용중\n";
        std::cout << "  ClientInfo 풀: " << g_clientInfoPool.GetInUseCount() << "/"
            << g_clientInfoPool.GetTotalAllocated() << " 사용중\n";
        std::cout << "  ProcessTask 풀: " << g_processTaskPool.GetInUseCount() << "/"
            << g_processTaskPool.GetTotalAllocated() << " 사용중\n";
        std::cout << "  SendTask 풀: " << g_sendTaskPool.GetInUseCount() << "/"
            << g_sendTaskPool.GetTotalAllocated() << " 사용중\n";

        g_lastBytesSent = g_totalBytesSent;
        g_lastBytesReceived = g_totalBytesReceived;
        g_lastStatsTime = now;
    }
}

// 타이머 초기화 함수
void InitializeTimers() {
    g_timersRunning = true;
    g_updateTimerThread = std::thread(UpdateTimerThread);
    g_statsTimerThread = std::thread(StatsTimerThread);
}

// 타이머 정리 함수
void CleanupTimers() {
    g_timersRunning = false;
    if (g_updateTimerThread.joinable()) {
        g_updateTimerThread.join();
    }
    if (g_statsTimerThread.joinable()) {
        g_statsTimerThread.join();
    }
}

// 패킷 생성 함수
bool CreatePacket(char* buffer, int bufferSize, PacketType type, const char* message, int& packetSize) {
    if (!buffer || !message) {
        std::cout << "CreatePacket: 잘못된 매개변수\n";
        return false;
    }

    int messageLen = (int)strlen(message);

    // 최대 메시지 크기 검사
    if (messageLen > MAX_MESSAGE_SIZE) {
        std::cout << "CreatePacket: 메시지가 너무 깁니다. (" << messageLen << " > " << MAX_MESSAGE_SIZE << ")\n";
        return false;
    }

    // null terminator 포함
    packetSize = PACKET_HEADER_SIZE + messageLen + 1;

    if (packetSize > bufferSize) {
        std::cout << "CreatePacket: 패킷 크기가 버퍼 크기를 초과합니다. (" << packetSize << " > " << bufferSize << ")\n";
        return false;
    }

    PacketHeader* header = (PacketHeader*)buffer;
    header->Size = packetSize;
    header->Type = (uint16_t)type;

    // 메시지 데이터 복사 (null terminator 포함)
    memcpy(buffer + PACKET_HEADER_SIZE, message, messageLen + 1);

    return true;
}

struct SendBuffer
{
    char Buffer[MAX_PACKET_SIZE] = { 0, };
    volatile long sendCompleteCnt;              // 할당은 한번만 하고 여러번 보내는 처리에서 사용
    SendBuffer() : sendCompleteCnt(0) {}
    ~SendBuffer() {}
};

// IOCP에서 사용할 오버랩 구조체
struct SendIOContext {
    OVERLAPPED overlapped;      // 반드시 첫 번째 멤버여야 함
    IOType ioType;              // I/O 작업 타입
    SOCKET socket;              // 해당 소켓
    WSABUF wsaBuf;              // 버퍼 정보
    SendBuffer* sendBuffer;         // 버퍼
    int clientId;               // 클라이언트 ID
};

struct ClientSocket
{
    SOCKET socket;
    int id;
};
ClientSocket g_clientSocketList[4096 * 4] = { 0, };
int g_clientsCnt = 0;

// Sender 태스크를 SenderThread로 전송하는 함수들
bool PostSenderPacket(SOCKET excludeSocket, SendBuffer* buffer, int packetSize) {
    if (g_senderCompletionPorts.empty()) {
        std::cout << "PostSenderPacket: Sender 완료 포트가 없습니다.\n";
        return false;
    }

    for (size_t i = 0; i < g_senderCompletionPorts.size(); ++i) {
        HANDLE port = g_senderCompletionPorts[i];
        SendTask* task = g_sendTaskPool.Allocate();
        task->type = SEND_BROADCAST;
        task->excludeSocket = excludeSocket;
        task->sendBuffer = buffer;
        task->packetSize = packetSize;

        BOOL result = PostQueuedCompletionStatus(port, 0, (ULONG_PTR)task, NULL);
        if (!result) {
            std::cout << "PostSenderPacket 실패: " << GetLastError() << "\n";
            g_sendTaskPool.Deallocate(task);
            return false;
        }
    }

    return true;
}

bool PostSenderSingle(SOCKET socket, int id, SendBuffer* buffer, int packetSize) {
    if (g_senderCompletionPorts.empty()) {
        std::cout << "PostSenderSingle: Sender 완료 포트가 없습니다.\n";
        return false;
    }

    int idx = id % static_cast<int>(g_senderCompletionPorts.size());
    HANDLE port = g_senderCompletionPorts[idx];

    SendTask* task = g_sendTaskPool.Allocate();
    task->type = SEND_SINGLE;
    task->socket = socket;
    task->id = id;
    task->sendBuffer = buffer;
    task->packetSize = packetSize;

    BOOL result = PostQueuedCompletionStatus(port, 0, (ULONG_PTR)task, NULL);
    if (!result) {
        std::cout << "PostSenderSingle 실패 (클라이언트 " << id << "): " << GetLastError() << "\n";
        g_sendTaskPool.Deallocate(task);
        return false;
    }

    return true;
}

bool PostSenderNew(SOCKET socket, int id, ClientInfo* client) {
    if (g_senderCompletionPorts.empty()) {
        std::cout << "PostSenderNew: Sender 완료 포트가 없습니다.\n";
        return false;
    }

    int idx = id % static_cast<int>(g_senderCompletionPorts.size());
    HANDLE port = g_senderCompletionPorts[idx];

    SendTask* task = g_sendTaskPool.Allocate();
    task->type = SEND_NEW;
    task->socket = socket;
    task->id = id;
    task->clientInfo = client;

    BOOL result = PostQueuedCompletionStatus(port, 0, (ULONG_PTR)task, NULL);
    if (!result) {
        std::cout << "PostSenderNew 실패 (클라이언트 " << id << "): " << GetLastError() << "\n";
        g_sendTaskPool.Deallocate(task);
        return false;
    }
    return true;
}

bool PostSenderRemove(SOCKET socket, int id) {
    if (g_senderCompletionPorts.empty()) {
        std::cout << "PostSenderRemove: Sender 완료 포트가 없습니다.\n";
        return false;
    }

    int idx = id % static_cast<int>(g_senderCompletionPorts.size());
    HANDLE port = g_senderCompletionPorts[idx];

    SendTask* task = g_sendTaskPool.Allocate();
    task->type = SEND_REMOVE;
    task->socket = socket;
    task->id = id;

    BOOL result = PostQueuedCompletionStatus(port, 0, (ULONG_PTR)task, NULL);
    if (!result) {
        std::cout << "PostSenderRemove 실패 (클라이언트 " << id << "): " << GetLastError() << "\n";
        g_sendTaskPool.Deallocate(task);
        return false;
    }
    return true;
}

// 클라이언트의 sendBuffer를 비동기 전송하는 함수 (SenderThread 사용)
bool FlushSendBuffer(ClientInfo* client) {
    SendPacketBuffer& buffer = client->sendBuffer;
    if (buffer.currentSize == 0) return true;

    SendBuffer* tempBuf = g_sendBufferPool.Allocate();
    memcpy(tempBuf->Buffer, buffer.data, buffer.currentSize);
    tempBuf->sendCompleteCnt = 1; // 단일 전송

    bool posted = PostSenderSingle(client->socket, client->id, tempBuf, buffer.currentSize);
    if (!posted) {
        std::cout << "FlushSendBuffer PostSenderSingle 실패 (클라이언트 " << client->id << ")\n";
        g_sendBufferPool.Deallocate(tempBuf);
        buffer.Reset();
        return false;
    }

    buffer.Reset();
    return true;
}

void AccumulateSendPacket(ClientInfo* client, const char* packetData, int packetSize) {
    SendPacketBuffer& buffer = client->sendBuffer;
    if (buffer.currentSize + packetSize > MAX_PACKET_SIZE) {
        FlushSendBuffer(client); // 4096 초과 시 플러시 후 누적
    }

    // 데이터 추가
    if (buffer.currentSize + packetSize > sizeof(buffer.data)) {
        std::cout << "송신 버퍼 오버플로우 (클라이언트 " << client->id << ")\n";
        return;
    }

    memcpy(buffer.data + buffer.currentSize, packetData, packetSize);
    buffer.currentSize += packetSize;
}

// 모든 클라이언트에게 패킷 브로드캐스트 (SenderThread 사용)
void BroadcastPacket_AccumulateSend(PacketType type, const char* message, SOCKET excludeSocket = INVALID_SOCKET) {
    if (!message) return;

    int packetSize;
    char packetBuf[MAX_PACKET_SIZE];

    // 패킷 데이터 생성
    if (!CreatePacket(packetBuf, sizeof(packetBuf), type, message, packetSize)) {
        std::cout << "BroadcastPacket: 패킷 생성 실패\n";
        return;
    }

    // 클라이언트 수집
    g_clientsCnt = 0;
    for (auto it = g_clients.begin(); it != g_clients.end(); ++it) {
        ClientInfo* client = it->second;
        if (client->socket != excludeSocket && client->isConnected) {
            AccumulateSendPacket(client, packetBuf, packetSize); // 패킷 누적
        }
    }
}

// 모든 클라이언트에게 패킷 브로드캐스트 (SenderThread 사용)
void BroadcastPacket(GGM_MMO_CPP::PacketType type, const std::vector<uint8_t>& data, SOCKET excludeSocket = INVALID_SOCKET) {
    int packetSize;

    SendBuffer* packetBuf = g_sendBufferPool.Allocate();

    NetBase::PacketBase packet;
    NetBase::PacketHeader header;
    header.Size = static_cast<uint16_t>(data.size() + sizeof(NetBase::PacketHeader));
    header.Type = static_cast<uint16_t>(type);

    packet.Write(header.Size);
    packet.Write(header.Type);

    std::vector<uint8_t> fullPacket = packet.GetPacketData();
    fullPacket.insert(fullPacket.end(), data.begin(), data.end());

    packetSize = static_cast<int>(fullPacket.size());

    memcpy(packetBuf->Buffer, reinterpret_cast<const char*>(fullPacket.data()),
        static_cast<int>(fullPacket.size()));

    // 대상 클라이언트 수 계산
    int totalTargets = 0;
    for (const auto& p : g_clients) {
        if (p.second->isConnected && p.second->socket != excludeSocket) {
            totalTargets++;
        }
    }

    if (totalTargets == 0) {
        g_sendBufferPool.Deallocate(packetBuf);
        return;
    }

    // SendBuffer 참조 카운트 설정
    packetBuf->sendCompleteCnt = totalTargets;

    // 모든 SenderThread에 브로드캐스트 태스크 전송
    bool allPosted = PostSenderPacket(excludeSocket, packetBuf, packetSize);

    if (!allPosted) {
        // 일부 실패 시에도 카운트는 유지, 완료에서 처리
        std::cout << "BroadcastPacket: 일부 Sender 포스트 실패\n";
    }
    return;
}

// 모든 클라이언트에게 패킷 브로드캐스트 (SenderThread 사용)
void BroadcastPacket(PacketType type, const char* message, SOCKET excludeSocket = INVALID_SOCKET) {
    if (!message) return;

    int packetSize;

    SendBuffer* packetBuf = g_sendBufferPool.Allocate();

    // 패킷 데이터 생성
    if (!CreatePacket(packetBuf->Buffer, sizeof(packetBuf->Buffer), type, message, packetSize)) {
        std::cout << "BroadcastPacket: 패킷 생성 실패\n";
        g_sendBufferPool.Deallocate(packetBuf);
        return;
    }

    // 대상 클라이언트 수 계산
    int totalTargets = 0;
    for (const auto& p : g_clients) {
        if (p.second->isConnected && p.second->socket != excludeSocket) {
            totalTargets++;
        }
    }

    if (totalTargets == 0) {
        g_sendBufferPool.Deallocate(packetBuf);
        return;
    }

    // SendBuffer 참조 카운트 설정
    packetBuf->sendCompleteCnt = totalTargets;

    // 모든 SenderThread에 브로드캐스트 태스크 전송
    bool allPosted = PostSenderPacket(excludeSocket, packetBuf, packetSize);

    if (!allPosted) {
        // 일부 실패 시에도 카운트는 유지, 완료에서 처리
        std::cout << "BroadcastPacket: 일부 Sender 포스트 실패\n";
    }
}

// 클라이언트 제거 (비동기 DisconnectEx 사용)
void RemoveClient(SOCKET clientSocket) {
    //EnterCriticalSection(&g_clientsCS);

    auto it = g_clients.find(clientSocket);
    if (it != g_clients.end()) {
        ClientInfo* client = it->second;
        if (!client->isConnected) {
            // 이미 종료된 경우 스킵
            //LeaveCriticalSection(&g_clientsCS);
            return;
        }

        std::cout << "클라이언트 " << client->id << " 연결 종료 시작 (DisconnectEx)\n";

        // 퇴장 메시지 브로드캐스트
        char goodbyeMsg[256];
        sprintf_s(goodbyeMsg, sizeof(goodbyeMsg),
            "클라이언트 %d님이 퇴장하였습니다.", client->id);

        GGM_MMO_CPP::C2SInGame::ChatAck ack;
        ack.message = goodbyeMsg;
        ack.chatType = ChatType::PACKET_CLIENT_LEAVE;
        ack.result = EResult::True;
        NetBase::PacketBase responsePacket;
        responsePacket.Write(ack);

        BroadcastPacket(PacketType::ChatAck, responsePacket.GetPacketData());

        // SenderRemove 전송
        PostSenderRemove(client->socket, client->id);

        // 비동기 DisconnectEx용 IOContext 생성
        IOContext* disconnectContext = g_ioContextPool.Allocate();
        ZeroMemory(disconnectContext, sizeof(IOContext));

        disconnectContext->ioType = IO_DISCONNECT;
        disconnectContext->socket = client->socket;
        disconnectContext->clientId = client->id;

        // DisconnectEx 호출 (flags: 0 for graceful shutdown, TF_REUSE_SOCKET 사용 안 함)
        BOOL result = g_lpfnDisconnectEx(client->socket, &disconnectContext->overlapped, 0, 0);

        if (!result) {
            DWORD error = WSAGetLastError();
            if (error != WSA_IO_PENDING) {
                std::cout << "DisconnectEx 실패 (클라이언트 " << client->id << "): " << error << " - 즉시 종료\n";
                closesocket(client->socket);  // fallback to closesocket
                g_ioContextPool.Deallocate(disconnectContext);
                g_clientInfoPool.Deallocate(client);
                g_clients.erase(it);
            }
        }

        client->isConnected = false;  // 즉시 연결 상태 변경 (재수신 방지)
    }
}

// AcceptEx 함수 포인터 초기화
bool InitializeAcceptEx() {
    DWORD dwBytes = 0;
    GUID guidAcceptEx = WSAID_ACCEPTEX;
    GUID guidGetAcceptExSockAddrs = WSAID_GETACCEPTEXSOCKADDRS;

    // AcceptEx 함수 포인터 가져오기
    int result = WSAIoctl(g_listenSocket, SIO_GET_EXTENSION_FUNCTION_POINTER,
        &guidAcceptEx, sizeof(guidAcceptEx),
        &g_lpfnAcceptEx, sizeof(g_lpfnAcceptEx),
        &dwBytes, NULL, NULL);

    if (result == SOCKET_ERROR) {
        std::cout << "AcceptEx 함수 포인터 가져오기 실패: " << WSAGetLastError() << "\n";
        return false;
    }

    // GetAcceptExSockAddrs 함수 포인터 가져오기
    result = WSAIoctl(g_listenSocket, SIO_GET_EXTENSION_FUNCTION_POINTER,
        &guidGetAcceptExSockAddrs, sizeof(guidGetAcceptExSockAddrs),
        &g_lpfnGetAcceptExSockAddrs, sizeof(g_lpfnGetAcceptExSockAddrs),
        &dwBytes, NULL, NULL);

    if (result == SOCKET_ERROR) {
        std::cout << "GetAcceptExSockAddrs 함수 포인터 가져오기 실패: " << WSAGetLastError() << "\n";
        return false;
    }

    std::cout << "AcceptEx 초기화 완료\n";
    return true;
}


// AcceptEx용 새 소켓 생성
SOCKET CreateAcceptSocket() {
    SOCKET sock = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (sock == INVALID_SOCKET) {
        std::cout << "AcceptEx용 소켓 생성 실패: " << WSAGetLastError() << "\n";
        return INVALID_SOCKET;
    }

    // 소켓 옵션 설정 (리스 소켓의 설정을 상속받도록)
    int opt = 1;
    setsockopt(sock, SOL_SOCKET, SO_REUSEADDR, (char*)&opt, sizeof(opt));

    return sock;
}

// AcceptEx 컨텍스트 생성 및 투입
bool PostAcceptEx() {
    IOContext* acceptContext = g_ioContextPool.Allocate();
    ZeroMemory(acceptContext, sizeof(IOContext));

    acceptContext->ioType = IO_ACCEPT;
    acceptContext->socket = g_listenSocket;
    acceptContext->acceptSocket = CreateAcceptSocket();

    if (acceptContext->acceptSocket == INVALID_SOCKET) {
        g_ioContextPool.Deallocate(acceptContext);
        return false;
    }

    // AcceptEx 호출
    DWORD bytesReceived = 0;
    BOOL result = g_lpfnAcceptEx(
        g_listenSocket,              // 리슨 소켓
        acceptContext->acceptSocket, // 새 소켓
        acceptContext->acceptBuffer, // 주소 정보 버퍼
        0,                           // 초기 데이터 수신하지 않음
        ACCEPT_ADDRESS_LENGTH,       // 로컬 주소 길이
        ACCEPT_ADDRESS_LENGTH,       // 원격 주소 길이
        &bytesReceived,              // 수신된 바이트 수
        &acceptContext->overlapped   // 오버랩 구조체
    );

    if (!result) {
        DWORD error = WSAGetLastError();
        if (error != WSA_IO_PENDING) {
            std::cout << "AcceptEx 호출 실패: " << error << "\n";
            closesocket(acceptContext->acceptSocket);
            g_ioContextPool.Deallocate(acceptContext);
            return false;
        }
    }

    g_acceptContexts.push_back(acceptContext); // 컨텍스트 관리
    return true;
}

// AcceptEx 완료 처리
void HandleAcceptCompletion(IOContext* acceptContext) {
    // 새 클라이언트 정보 생성
    g_clientCounter++;

    // 클라이언트 정보 생성 및 초기화
    ClientInfo* newClient = g_clientInfoPool.Allocate();
    ZeroMemory(newClient, sizeof(ClientInfo));

    newClient->socket = acceptContext->acceptSocket;
    newClient->id = g_clientCounter;
    newClient->isConnected = true;

    // 소켓 컨텍스트 상속
    int result = setsockopt(acceptContext->acceptSocket, SOL_SOCKET, SO_UPDATE_ACCEPT_CONTEXT,
        (char*)&g_listenSocket, sizeof(g_listenSocket));
    if (result == SOCKET_ERROR) {
        std::cout << "SO_UPDATE_ACCEPT_CONTEXT 설정 실패: " << WSAGetLastError() << "\n";
        g_clientInfoPool.Deallocate(newClient);
        closesocket(acceptContext->acceptSocket);
        return;
    }

    // 수신용 IOContext 초기화
    ZeroMemory(&newClient->recvContext, sizeof(IOContext));
    newClient->recvContext.ioType = IO_RECV;
    newClient->recvContext.socket = newClient->socket;
    newClient->recvContext.clientId = newClient->id;
    newClient->recvContext.wsaBuf.buf = newClient->recvContext.buffer;
    newClient->recvContext.wsaBuf.len = sizeof(newClient->recvContext.buffer) - 1;

    // 클라이언트 소켓을 완료 포트에 연결
    HANDLE hResult = CreateIoCompletionPort((HANDLE)newClient->socket, g_hCompletionPort,
        (ULONG_PTR)newClient, 0);
    if (hResult == NULL) {
        std::cout << "클라이언트 소켓을 완료 포트에 연결 실패: " << GetLastError() << "\n";
        g_clientInfoPool.Deallocate(newClient);
        closesocket(acceptContext->acceptSocket);
        return;
    }

    // 클라이언트 주소 정보 가져오기
    sockaddr* localAddr = NULL;
    sockaddr* remoteAddr = NULL;
    int localAddrLen = 0;
    int remoteAddrLen = 0;

    // AcceptEx 완료 후 주소 정보 추출
    g_lpfnGetAcceptExSockAddrs(
        acceptContext->acceptBuffer, // AcceptEx 버퍼
        0,                           // 수신 데이터 길이
        ACCEPT_ADDRESS_LENGTH,       // 로컬 주소 길이
        ACCEPT_ADDRESS_LENGTH,       // 원격 주소 길이
        &localAddr,                  // 로컬 주소
        &localAddrLen,               // 로컬 주소 길이
        &remoteAddr,                 // 원격 주소
        &remoteAddrLen               // 원격 주소 길이
    );

    // 클라이언트 IP 정보 출력
    if (remoteAddr && remoteAddr->sa_family == AF_INET) {
        sockaddr_in* remoteAddrIn = (sockaddr_in*)remoteAddr;
        char clientIP[INET_ADDRSTRLEN];
        inet_ntop(AF_INET, &remoteAddrIn->sin_addr, clientIP, INET_ADDRSTRLEN);
    }

    // 클라이언트 맵에 추가
    PostProcessNew(newClient->socket, newClient->id, newClient);

    // 첫 번째 비동기 수신 시작
    DWORD bytesReceived = 0;
    DWORD flags = 0;
    result = WSARecv(newClient->socket, &newClient->recvContext.wsaBuf, 1,
        &bytesReceived, &flags, &newClient->recvContext.overlapped, NULL);

    if (result == SOCKET_ERROR) {
        DWORD error = WSAGetLastError();
        if (error != WSA_IO_PENDING) {
            std::cout << "클라이언트 " << newClient->id << " 초기 수신 설정 실패: " << error << "\n";
            PostProcessRemove(newClient->socket, newClient->id);
        }
    }

    // 새로운 AcceptEx 투입 (다음 연결 대기)
    PostAcceptEx();
}

// DisconnectEx 함수 포인터 초기화
bool InitializeDisconnectEx() {
    DWORD dwBytes = 0;
    GUID guidDisconnectEx = WSAID_DISCONNECTEX;

    int result = WSAIoctl(g_listenSocket, SIO_GET_EXTENSION_FUNCTION_POINTER,
        &guidDisconnectEx, sizeof(guidDisconnectEx),
        &g_lpfnDisconnectEx, sizeof(g_lpfnDisconnectEx),
        &dwBytes, NULL, NULL);

    if (result == SOCKET_ERROR) {
        std::cout << "DisconnectEx 함수 포인터 가져오기 실패: " << WSAGetLastError() << "\n";
        return false;
    }

    std::cout << "DisconnectEx 초기화 완료\n";
    return true;
}

// 패킷 파싱 함수
bool ParsePacket(const char* buffer, int bufferSize, PacketType& type, std::string& message) {
    if (!buffer || bufferSize < PACKET_HEADER_SIZE) {
        std::cout << "ParsePacket: 잘못된 매개변수 또는 패킷 크기 부족\n";
        return false;
    }

    const PacketHeader* header = (const PacketHeader*)buffer;

    if (header->Size != bufferSize) {
        std::cout << "ParsePacket: 패킷 크기 불일치: 헤더=" << header->Size << ", 실제=" << bufferSize << "\n";
        return false;
    }

    // 패킷 타입 유효성 검사
    if (!IsValidPacketType((PacketType)header->Type)) {
        std::cout << "ParsePacket: 유효하지 않은 패킷 타입: " << header->Type << "\n";
        return false;
    }

    type = (PacketType)header->Type;

    int messageSize = bufferSize - PACKET_HEADER_SIZE;
    if (messageSize > 0) {
        // 안전검사: 메시지 크기가 적절한지 확인
        if (messageSize <= MAX_MESSAGE_SIZE + 1) { // +1 for null terminator
            message.assign(buffer + PACKET_HEADER_SIZE, messageSize - 1); // null terminator 제외
        }
        else {
            std::cout << "ParsePacket: 메시지 크기 초과: " << messageSize << "\n";
            return false;
        }
    }
    else {
        message.clear();
    }

    return true;
}

// 패킷 처리 함수 (ProcessThread에서 호출)
void ProcessCompletePacket(SOCKET socket, int clientId, char* packetData, int packetSize) {
    auto startTime = std::chrono::high_resolution_clock::now();
    PacketType type;
    std::string message;

    if (!ParsePacket(packetData, packetSize, type, message)) {
        std::cout << "패킷 파싱 실패 (클라이언트 " << clientId << ")\n";
        return;
    }

    // 수신된 패킷 통계 업데이트
    g_totalPacketsReceived++;
    g_totalBytesReceived += packetSize;

    switch (type) {
    case PacketType::ChatReq: {
        NetBase::PacketBase packet;

        std::vector<uint8_t> data(reinterpret_cast<const uint8_t*>(packetData + PACKET_HEADER_SIZE),
            reinterpret_cast<const uint8_t*>(packetData + packetSize));
        packet.SetPacketData(data);
        GGM_MMO_CPP::C2SInGame::ChatReq req = packet.Read<GGM_MMO_CPP::C2SInGame::ChatReq>();

        GGM_MMO_CPP::ChatType ctype = req.chatType;
        char broadcastMsg[1280];
        sprintf_s(broadcastMsg, sizeof(broadcastMsg), "클라이언트 %d: %s",
            clientId, req.message.c_str());

        switch (ctype) {
        case ChatType::PACKET_CHAT_MESSAGE: {
            // 채팅 메시지 처리

            std::cout << broadcastMsg << "\n";

            PacketType type = PacketType::ChatAck;

            GGM_MMO_CPP::C2SInGame::ChatAck ack;
            ack.message = broadcastMsg;
            ack.chatType = ChatType::PACKET_CHAT_MESSAGE;
            ack.result = EResult::True;
            NetBase::PacketBase responsePacket;
            responsePacket.Write(ack);

            BroadcastPacket(PacketType::ChatAck, responsePacket.GetPacketData());
            break;
        }

        case ChatType::PACKET_SYSTEM_MESSAGE: {
            // 시스템 메시지 처리
            std::cout << "[시스템] 클라이언트 " << clientId << ": " << message << "\n";

            GGM_MMO_CPP::C2SInGame::ChatAck ack;
            ack.message = broadcastMsg;
            ack.chatType = ChatType::PACKET_SYSTEM_MESSAGE;
            ack.result = EResult::True;
            NetBase::PacketBase responsePacket;
            responsePacket.Write(ack);

            BroadcastPacket(PacketType::ChatAck, responsePacket.GetPacketData());
            break;
        }
        }
        break; // ChatReq case를 마무리
    }
    default:
        std::cout << "알 수 없는 패킷 타입: " << PacketTypeToString(type) << " (클라이언트 " << clientId << ")\n";
        break;
    }

    // 패킷 처리 시간 계산 및 통계 업데이트
    auto endTime = std::chrono::high_resolution_clock::now();
    auto processingTime = std::chrono::duration_cast<std::chrono::microseconds>(endTime - startTime).count();
    UpdatePacketStats(type, processingTime);
}

// 수신된 데이터를 패킷 버퍼에 추가하고 완성된 패킷 처리
void ProcessReceivedData(ClientInfo* client, char* data, int dataSize) {
    PacketBuffer& buffer = client->packetBuffer;

    // 받은 데이터를 버퍼에 추가
    if (buffer.currentSize + dataSize > MAX_PACKET_SIZE) {
        std::cout << "패킷 버퍼 오버플로우 (클라이언트 " << client->id << ")\n";
        PostProcessRemove(client->socket, client->id);
        return;
    }

    memcpy(buffer.data + buffer.currentSize, data, dataSize);
    buffer.currentSize += dataSize;

    // 패킷 처리 루프 (무한 루프 방지)
    int processedPackets = 0;
    while (buffer.currentSize > 0 && processedPackets < MAX_PACKETS_PER_CALL) {
        // 헤더를 아직 받지 못했다면
        if (!buffer.headerReceived) {
            if (buffer.currentSize >= PACKET_HEADER_SIZE) {
                PacketHeader* header = (PacketHeader*)buffer.data;
                buffer.expectedSize = header->Size;
                buffer.headerReceived = true;

                // 패킷 크기 유효성 검사
                if (buffer.expectedSize < PACKET_HEADER_SIZE ||
                    buffer.expectedSize > MAX_PACKET_SIZE) {
                    std::cout << "잘못된 패킷 크기: " << buffer.expectedSize
                        << " (클라이언트 " << client->id << ")\n";
                    PostProcessRemove(client->socket, client->id);
                    return;
                }
            }
            else {
                // 헤더가 완성되지 않았으므로 더 기다림
                break;
            }
        }

        // 완전한 패킷이 도착했는지 확인
        if (buffer.currentSize >= buffer.expectedSize) {
            // 완성된 패킷을 ProcessThread로 전송
            PostProcessPacket(client->socket, client->id, buffer.data, buffer.expectedSize);

            // 처리된 패킷 데이터 제거
            int remainingSize = buffer.currentSize - buffer.expectedSize;
            if (remainingSize > 0) {
                memmove(buffer.data, buffer.data + buffer.expectedSize, remainingSize);
            }

            // 순서 수정: Reset 먼저, 크기 설정 나중에
            buffer.Reset();
            buffer.currentSize = remainingSize;

            processedPackets++;
        }
        else {
            // 패킷이 완성되지 않았으므로 더 기다림
            break;
        }
    }

    // 너무 많은 패킷을 처리한 경우 경고
    if (processedPackets >= MAX_PACKETS_PER_CALL) {
        std::cout << "한 번에 너무 많은 패킷 처리 (클라이언트 " << client->id << "): " << processedPackets << "\n";
    }
}

// 패킷을 ProcessThread로 전송 (image_bcb310.png)
bool PostProcessPacket(SOCKET socket, int id, char* packetData, int packetSize) {
    ProcessTask* task = g_processTaskPool.Allocate();
    task->type = TASK_PROCESS_PACKET;
    task->id = id;
    task->socket = socket;
    task->packetSize = packetSize;

    memcpy(task->packetData, packetData, packetSize);

    BOOL result = PostQueuedCompletionStatus(g_hProcessCompletionPort, 0, (ULONG_PTR)task, NULL);
    if (!result) {
        std::cout << "PostProcessPacket 실패 (클라이언트 " << id << "): " << GetLastError() << "\n";
        g_processTaskPool.Deallocate(task);
        return false;
    }

    return true;
}
bool PostProcessNew(SOCKET socket, int id, ClientInfo* newClient)
{
    ProcessTask* task = g_processTaskPool.Allocate();
    task->type = TASK_PROCESS_NEW_CLIENT;
    task->id = id;
    task->socket = socket;
    task->packetSize = 0;
    task->clientInfo = newClient;
    BOOL result = PostQueuedCompletionStatus(g_hProcessCompletionPort, 0, (ULONG_PTR)task, NULL);
    if (!result) {
        std::cout << "PostProcessNew 실패 (클라이언트 " << id << "): " << GetLastError() << "\n";
        g_processTaskPool.Deallocate(task);
        return false;
    }
    return true;
}
bool PostProcessRemove(SOCKET socket, int id)
{
    ProcessTask* task = g_processTaskPool.Allocate();
    task->type = TASK_PROCESS_REMOVE_CLIENT;
    task->id = id;
    task->socket = socket;
    task->packetSize = 0;

    BOOL result = PostQueuedCompletionStatus(g_hProcessCompletionPort, 0, (ULONG_PTR)task, NULL);
    if (!result) {
        std::cout << "PostProcessRemove 실패 (클라이언트 " << id << "): " << GetLastError() << "\n";
        g_processTaskPool.Deallocate(task);
        return false;
    }
    return true;
}

// IOCP 프로세스 스레드
DWORD WINAPI ProcessThread(LPVOID lpParam) {
    DWORD threadId = GetCurrentThreadId();
    std::cout << "[Processer " << threadId << "] 시작됨\n";

    DWORD bytesTransferred = 0;
    ULONG_PTR completionKey = 0;
    LPOVERLAPPED lpOverlapped = NULL;

    while (g_serverRunning) {
        BOOL result = GetQueuedCompletionStatus(g_hProcessCompletionPort, &bytesTransferred,
            &completionKey, &lpOverlapped, INFINITE);

        if (!result) {
            DWORD error = GetLastError();
            std::cout << "[Processer " << threadId << "] GetQueuedCompletionStatus 실패: " << error << "\n";
            continue;
        }

        ProcessTask* task = (ProcessTask*)completionKey;
        if (!task) {
            std::cout << "[Processer " << threadId << "] 잘못된 태스크\n";
            continue;
        }

        if (task->type == TASK_PROCESS_PACKET) {
            ProcessCompletePacket(task->socket, task->id, task->packetData, task->packetSize);
        }
        else if (task->type == TASK_PROCESS_NEW_CLIENT) {
            //g_clients[task->socket] = newClient;
            g_clients[task->socket] = task->clientInfo;

            // 입장 메시지 브로드캐스트
            char welcomeMsg[256];
            sprintf_s(welcomeMsg, sizeof(welcomeMsg),
                "클라이언트 %d님이 입장하였습니다.", g_clients[task->socket]->id);

            GGM_MMO_CPP::C2SInGame::ChatAck ack;
            ack.message = welcomeMsg;
            ack.chatType = ChatType::PACKET_CLIENT_JOIN;
            ack.result = EResult::True;
            NetBase::PacketBase responsePacket;
            responsePacket.Write(ack);

            BroadcastPacket(PacketType::ChatAck, responsePacket.GetPacketData());

            // SenderNew 전송
            PostSenderNew(task->socket, task->id, task->clientInfo);
        }
        else if (task->type == TASK_PROCESS_REMOVE_CLIENT) {
            RemoveClient(task->socket);
        }
        else if (task->type == TASK_FLUSH_SEND_BUFFERS) {
            //모든 클라이언트의 누적 패킷 플러시
            for (auto& pair : g_clients) {
                ClientInfo* client = pair.second;
                if (client->isConnected) {
                    FlushSendBuffer(client);
                }
            }
        }
        else {
            std::cout << "[Processer " << threadId << "] 알 수 없는 태스크 타입: " << task->type << "\n";
        }

        g_processTaskPool.Deallocate(task);
    }

    std::cout << "[Processer " << threadId << "] 종료됨\n";
    return 0;
}

// IOCP 워커 스레드
DWORD WINAPI WorkerThread(LPVOID lpParam) {
    DWORD threadId = GetCurrentThreadId();
    std::cout << "[Worker " << threadId << "] 시작됨\n";

    DWORD bytesTransferred = 0;
    ClientInfo* clientInfo = NULL;
    LPOVERLAPPED lpOverlapped = NULL;

    while (g_serverRunning) {
        BOOL result = GetQueuedCompletionStatus(g_hCompletionPort, &bytesTransferred,
            (PULONG_PTR)&clientInfo, &lpOverlapped, INFINITE);

        if (!result) {
            DWORD error = GetLastError();
            if (error == WAIT_TIMEOUT) {
                continue; // 타임아웃은 정상, 계속 진행
            }

            // 에러 발생 시 처리
            if (lpOverlapped && clientInfo) {
                IOContext* ioContext = (IOContext*)lpOverlapped;
                std::cout << "[Worker " << threadId << "] I/O 실패 (클라이언트 " << clientInfo->id << "): " << error << "\n";
                if (ioContext->ioType == IO_ACCEPT) {
                    //AcceptEx 실패 처리
                    closesocket(ioContext->acceptSocket);
                    g_ioContextPool.Deallocate(ioContext);
                    //새로운 AcceptEx 투입
                    PostAcceptEx();
                }
                else if (ioContext->ioType == IO_RECV) {
                    PostProcessRemove(clientInfo->socket, clientInfo->id);
                }
                else if (ioContext->ioType == IO_SEND) {
                    SendIOContext* sendIOContext = (SendIOContext*)ioContext;
                    if (InterlockedDecrement(&(sendIOContext->sendBuffer->sendCompleteCnt)) == 0) {
                        g_sendBufferPool.Deallocate(sendIOContext->sendBuffer);
                    }
                    g_sendIOContextPool.Deallocate(sendIOContext);
                }
                else {
                    g_ioContextPool.Deallocate(ioContext);
                }
            }
            continue;
        }

        // 널 포인터 체크 (정상 완료된 경우)
        if (!lpOverlapped) {
            std::cout << "[Worker " << threadId << "] 잘못된 완료 패턴\n";
            continue;
        }

        IOContext* ioContext = (IOContext*)lpOverlapped;

        switch (ioContext->ioType) {
        case IO_ACCEPT: {
            // Accept 완료 처리
            std::cout << "[Worker " << threadId << "] AcceptEx 완료\n";
            HandleAcceptCompletion(ioContext);
            //AcceptEx 컨텍스트 정리
            g_ioContextPool.Deallocate(ioContext);
            break;
        }
        case IO_RECV: {
            if (bytesTransferred == 0) {
                // 클라이언트가 연결을 종료함
                std::cout << "[Worker " << threadId << "] 클라이언트 " << clientInfo->id << " 정상 종료\n";
                PostProcessRemove(clientInfo->socket, clientInfo->id);
                continue;
            }

            //받은 데이터를 패킷 버퍼로 처리
            ProcessReceivedData(clientInfo, ioContext->buffer, bytesTransferred);

            //다음 수신을 위해 컨텍스트 재설정
            ZeroMemory(&ioContext->overlapped, sizeof(OVERLAPPED));
            ioContext->wsaBuf.buf = ioContext->buffer;
            ioContext->wsaBuf.len = sizeof(ioContext->buffer) - 1;

            //다시 비동기 수신 시작
            DWORD bytesReceived = 0;
            DWORD flags = 0;
            int recvResult = WSARecv(clientInfo->socket, &ioContext->wsaBuf, 1,
                &bytesReceived, &flags, &ioContext->overlapped, NULL);

            if (recvResult == SOCKET_ERROR) {
                DWORD error = WSAGetLastError();
                if (error != WSA_IO_PENDING) {
                    std::cout << "[Worker " << threadId << "] WSARecv 실패 (클라이언트 " << clientInfo->id << "): " << error << "\n";
                    PostProcessRemove(clientInfo->socket, clientInfo->id);
                }
            }
            break;
        }
        case IO_SEND: {
            //송신 완료 - 통계 업데이트 (비동기 완료된 경우)
            g_totalPacketsSent++;
            g_totalBytesSent += ioContext->wsaBuf.len;

            //송신 완료된 클라이언트 수 감소 및 버퍼 메모리 정리
            SendIOContext* sendIOContext = (SendIOContext*)ioContext;
            if (InterlockedDecrement(&(sendIOContext->sendBuffer->sendCompleteCnt)) == 0) {
                g_sendBufferPool.Deallocate(sendIOContext->sendBuffer);
            }

            //동적 할당된 컨텍스트 해제
            g_sendIOContextPool.Deallocate(sendIOContext);
            break;
        }
        case IO_DISCONNECT: {
            std::cout << "[Worker " << threadId << "] DisconnectEx 완료\n";

            //실제 소켓 닫기와 정리
            auto it = g_clients.find(ioContext->socket);
            if (it != g_clients.end()) {
                ClientInfo* client = it->second;
                closesocket(client->socket); // 이제 안전하게 closesocket
                g_clientInfoPool.Deallocate(client);
                g_clients.erase(it);
            }

            g_ioContextPool.Deallocate(ioContext); // 컨텍스트 정리
            break;
        }
        default:
            std::cout << "[Worker " << threadId << "] 알 수 없는 I/O 타입: " << ioContext->ioType << "\n";
            break;
        }
    }

    std::cout << "[Worker " << threadId << "] 종료됨\n";
    return 0;
}
// IOCP Sender 스레드 (태스크 처리)
DWORD WINAPI SenderThread(LPVOID lpParam) {
    HANDLE senderCompletionPort = (HANDLE)lpParam;
    DWORD threadId = GetCurrentThreadId();
    std::cout << "[Sender " << threadId << "] 시작됨 (CompletionPort: " << senderCompletionPort << ")\n";

    std::map<SOCKET, ClientInfo*> _clients;

    DWORD bytesTransferred = 0;
    ULONG_PTR completionKey = 0;
    LPOVERLAPPED lpOverlapped = NULL;

    while (g_serverRunning) {
        BOOL result = GetQueuedCompletionStatus(senderCompletionPort, &bytesTransferred,
            &completionKey, &lpOverlapped, INFINITE);

        if (!result) {
            DWORD error = GetLastError();
            std::cout << "[Sender " << threadId << "] GetQueuedCompletionStatus 실패: " << error << "\n";
            continue;
        }

        SendTask* task = (SendTask*)completionKey;
        if (!task) {
            std::cout << "[Sender " << threadId << "] 잘못된 태스크\n";
            continue;
        }

        switch (task->type) {
        case SEND_BROADCAST: {
            SOCKET exclude = task->excludeSocket;
            SendBuffer* buf = task->sendBuffer;
            int psize = task->packetSize;
            for (const auto& p : _clients) {
                SOCKET sock = p.first;
                ClientInfo* cli = p.second;
                if (!cli->isConnected || sock == exclude) continue;

                SendIOContext* sendContext = g_sendIOContextPool.Allocate();
                ZeroMemory(sendContext, sizeof(SendIOContext));

                sendContext->ioType = IO_SEND;
                sendContext->socket = sock;
                sendContext->clientId = cli->id;
                sendContext->sendBuffer = buf;
                sendContext->wsaBuf.buf = buf->Buffer;
                sendContext->wsaBuf.len = (DWORD)psize;

                DWORD bytesSent = 0;
                int wsares = WSASend(sock, &sendContext->wsaBuf, 1, &bytesSent, 0, &sendContext->overlapped, NULL);
                if (wsares == SOCKET_ERROR) {
                    DWORD error = WSAGetLastError();
                    if (error != WSA_IO_PENDING) {
                        std::cout << "Sender WSASend 실패 (클라이언트 " << cli->id << "): " << error << "\n";
                        if (InterlockedDecrement(&(sendContext->sendBuffer->sendCompleteCnt)) == 0) {
                            g_sendBufferPool.Deallocate(sendContext->sendBuffer);
                        }
                        g_sendIOContextPool.Deallocate(sendContext);
                    }
                }
            }
            break;
        }
        case SEND_SINGLE: {
            SOCKET sock = task->socket;
            int cid = task->id;
            SendBuffer* buf = task->sendBuffer;
            int psize = task->packetSize;

            SendIOContext* sendContext = g_sendIOContextPool.Allocate();
            ZeroMemory(sendContext, sizeof(SendIOContext));

            sendContext->ioType = IO_SEND;
            sendContext->socket = sock;
            sendContext->clientId = cid;
            sendContext->sendBuffer = buf;
            sendContext->wsaBuf.buf = buf->Buffer;
            sendContext->wsaBuf.len = (DWORD)psize;

            DWORD bytesSent = 0;
            int wsares = WSASend(sock, &sendContext->wsaBuf, 1, &bytesSent, 0, &sendContext->overlapped, NULL);
            if (wsares == SOCKET_ERROR) {
                DWORD error = WSAGetLastError();
                if (error != WSA_IO_PENDING) {
                    std::cout << "Sender WSASend 실패 (클라이언트 " << cid << "): " << error << "\n";
                    if (InterlockedDecrement(&(sendContext->sendBuffer->sendCompleteCnt)) == 0) {
                        g_sendBufferPool.Deallocate(sendContext->sendBuffer);
                    }
                    g_sendIOContextPool.Deallocate(sendContext);
                }
            }
            break;
        }
        case SEND_NEW: {
            _clients[task->socket] = task->clientInfo;
            break;
        }
        case SEND_REMOVE: {
            _clients.erase(task->socket);
            std::cout << "[Sender " << threadId << "] 클라이언트 " << task->id << " 제거\n";
            break;
        }
        default: {
            std::cout << "[Sender " << threadId << "] 알 수 없는 Sender 태스크 타입: " << task->type << "\n";
            break;
        }
        }

        g_sendTaskPool.Deallocate(task);
    }

    std::cout << "[Sender " << threadId << "] 종료됨\n";
    return 0;
}

int main()
{
    SetConsoleOutputCP(CP_UTF8);
    SetConsoleCP(CP_UTF8);
    system("chcp 65001 > nul");

    /*
    Visual Studio에서 솔루션 탐색기(Solution Explorer)에서 프로젝트를 선택합니다.
    프로젝트를 우클릭하고 **속성(Properties)**을 선택합니다.
    속성 창에서 **구성 속성(Configuration Properties) -> C/C++ -> 명령줄(Command Line)**으로 이동합니다.
    추가 옵션(Additional Options) 텍스트 상자에 다음을 입력: /utf-8
    확인(OK) 또는 적용(Apply)버튼을 클릭하여 설정을 저장합니다.
    프로젝트를 다시 빌드하여 변경 사항을 적용합니다.
    */
    // Winsock 초기화
    WSADATA wsaData;
    if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0) {
        std::cerr << "WSA 초기화 실패\n";
        return 1;
    }

    //// Critical Section 초기화
    //InitializeCriticalSection(&g_clientsCS);

    // 완료 포트 생성 (시스템 코어 수만큼 동시 스레드 허용)
    SYSTEM_INFO sysInfo;
    GetSystemInfo(&sysInfo);
    g_hCompletionPort = CreateIoCompletionPort(INVALID_HANDLE_VALUE, NULL, 0,
        sysInfo.dwNumberOfProcessors);
    if (g_hCompletionPort == NULL) {
        std::cerr << "완료 포트 생성 실패\n";
        WSACleanup();
        return 1;
    }

    g_hProcessCompletionPort = CreateIoCompletionPort(INVALID_HANDLE_VALUE, NULL, 0, 1);
    if (g_hProcessCompletionPort == NULL) {
        std::cerr << "프로세스 완료 포트 생성 실패\n";
        CloseHandle(g_hCompletionPort);
        WSACleanup();
        return 1;
    }

    // 리슨 소켓 생성
    g_listenSocket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (g_listenSocket == INVALID_SOCKET) {
        std::cerr << "소켓 생성 실패\n";
        CloseHandle(g_hCompletionPort);
        CloseHandle(g_hProcessCompletionPort);
        WSACleanup();
        return 1;
    }

    // 소켓 옵션 설정 (포트 재사용 허용)
    int opt = 1;
    if (setsockopt(g_listenSocket, SOL_SOCKET, SO_REUSEADDR, (char*)&opt, sizeof(opt)) == SOCKET_ERROR) {
        std::cerr << "SO_REUSEADDR 설정 실패: " << WSAGetLastError() << "\n";
    }

    // 서버 주소 설정
    sockaddr_in serverAddr;
    serverAddr.sin_family = AF_INET;
    serverAddr.sin_addr.s_addr = INADDR_ANY;
    serverAddr.sin_port = htons(8080);

    // 바인드
    if (bind(g_listenSocket, (sockaddr*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR) {
        std::cerr << "바인드 실패\n";
        closesocket(g_listenSocket);
        CloseHandle(g_hCompletionPort);
        CloseHandle(g_hProcessCompletionPort);
        WSACleanup();
        return 1;
    }

    // 리슨
    if (listen(g_listenSocket, SOMAXCONN) == SOCKET_ERROR) {
        std::cerr << "리슨 실패\n";
        closesocket(g_listenSocket);
        CloseHandle(g_hCompletionPort);
        CloseHandle(g_hProcessCompletionPort);
        WSACleanup();
        return 1;
    }

    // AcceptEx 초기화
    if (!InitializeAcceptEx()) {
        std::cerr << "AcceptEx 초기화 실패\n";
        closesocket(g_listenSocket);
        CloseHandle(g_hCompletionPort);
        CloseHandle(g_hProcessCompletionPort);
        WSACleanup();
        return 1;
    }

    // DisconnectEx 초기화
    if (!InitializeDisconnectEx()) {
        std::cerr << "DisconnectEx 초기화 실패\n";
        closesocket(g_listenSocket);
        CloseHandle(g_hCompletionPort);
        CloseHandle(g_hProcessCompletionPort);
        WSACleanup();
        return 1;
    }

    // 리슨 소켓을 완료 포트에 연결 (AcceptEx 완료 처리용)
    HANDLE hResult = CreateIoCompletionPort((HANDLE)g_listenSocket, g_hCompletionPort, 0, 0);
    if (hResult == NULL) {
        std::cerr << "리슨 소켓을 완료 포트에 연결 실패\n";
        closesocket(g_listenSocket);
        CloseHandle(g_hCompletionPort);
        CloseHandle(g_hProcessCompletionPort);
        WSACleanup();
        return 1;
    }

    std::cout << "=== AcceptEx 기반 IOCP 다중 클라이언트 채팅 서버 시작 ===\n";
    std::cout << "포트 8080에서 대기 중...\n";
    std::cout << "워커 스레드 수: " << sysInfo.dwNumberOfProcessors * 2 << "\n";
    std::cout << "IOCP 동시 실행 스레드 수: " << sysInfo.dwNumberOfProcessors << "\n";
    std::cout << "패킷 헤더 크기: " << PACKET_HEADER_SIZE << " 바이트\n";
    std::cout << "최대 패킷 크기: " << MAX_PACKET_SIZE << " 바이트\n";
    std::cout << "최대 메시지 크기: " << MAX_MESSAGE_SIZE << " 바이트\n";
    std::cout << "AcceptEx 컨텍스트 수: " << g_acceptContextCount << "\n";
    std::cout << "Sender 스레드 수: " << sysInfo.dwNumberOfProcessors << "\n";
    std::cout << "서버 종료하려면 아무 키나 누르세요.\n\n";

    // 워커 스레드 생성 (CPU 코어 수의 2배)
    std::vector<HANDLE> workerThreads;
    for (DWORD i = 0; i < sysInfo.dwNumberOfProcessors * 2; i++) {
        HANDLE hThread = CreateThread(NULL, 0, WorkerThread, NULL, 0, NULL);
        if (hThread != NULL) {
            workerThreads.push_back(hThread);
        }
    }

    HANDLE hProcessThread = CreateThread(NULL, 0, ProcessThread, NULL, 0, NULL);
    if (hProcessThread == NULL) {
        std::cerr << "프로세스 스레드 생성 실패\n";
        closesocket(g_listenSocket);
        CloseHandle(g_hCompletionPort);
        CloseHandle(g_hProcessCompletionPort);
        WSACleanup();
        return 1;
    }

    std::vector<HANDLE> senderThreads;
    for (DWORD i = 0; i < sysInfo.dwNumberOfProcessors; i++) {
        HANDLE senderCompletionPort = CreateIoCompletionPort(INVALID_HANDLE_VALUE, NULL, 0, 1);
        if (senderCompletionPort == NULL) {
            std::cerr << "Sender 완료 포트 생성 실패\n";
            // 기존 핸들 정리 (생략)
            return 1;
        }

        g_senderCompletionPorts.push_back(senderCompletionPort);
        HANDLE hThread = CreateThread(NULL, 0, SenderThread, senderCompletionPort, 0, NULL);
        if (hThread != NULL) {
            senderThreads.push_back(hThread);
        }
    }

    // 초기 AcceptEx 컨텍스트들 투입
    for (int i = 0; i < g_acceptContextCount; i++) {
        if (!PostAcceptEx()) {
            std::cout << "초기 AcceptEx 투입 실패 (인덱스 " << i << ")\n";
        }
    }

    // 통계 및 업데이트 타이머 시작
    InitializeTimers();

    // 키보드 입력 대기
    _getch();

    // 서버 종료 작업
    std::cout << "\n서버를 종료합니다...\n";
    g_serverRunning = false;

    // 타이머 정리
    CleanupTimers();

    // 리슨 소켓 종료
    closesocket(g_listenSocket);

    // 모든 클라이언트 연결 종료
    //EnterCriticalSection(&g_clientsCS);
    for (auto& pair : g_clients) {
        if (pair.second->isConnected) {
            RemoveClient(pair.second->socket); // DisconnectEx 사용
        }
    }

    // 워커 스레드들 종료 대기
    for (HANDLE hThread : workerThreads) {
        WaitForSingleObject(hThread, 3000);
        CloseHandle(hThread);
    }

    // 센더 스레드들 종료 대기
    for (HANDLE hThread : senderThreads) {
        WaitForSingleObject(hThread, 3000);
        CloseHandle(hThread);
    }

    // 센더 완료 포트들 정리
    for (HANDLE hPort : g_senderCompletionPorts) {
        CloseHandle(hPort);
    }

    WaitForSingleObject(hProcessThread, 3000);
    CloseHandle(hProcessThread);

    // AcceptEx 컨텍스트 정리
    for (IOContext* context : g_acceptContexts) {
        if (context->acceptSocket != INVALID_SOCKET) {
            closesocket(context->acceptSocket);
        }
        g_ioContextPool.Deallocate(context); // 컨텍스트 정리
    }
    g_acceptContexts.clear();

    CloseHandle(g_hCompletionPort);
    CloseHandle(g_hProcessCompletionPort);
    WSACleanup();

    std::cout << "서버가 종료되었습니다.\n";
    return 0;
}
