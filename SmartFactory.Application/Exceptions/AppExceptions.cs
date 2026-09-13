// 应用层异常统一分类，便于 ViewModel 显示友好提示并保留日志上下文。
namespace SmartFactory.Application.Exceptions;

/// <summary>SmartFactory 应用异常基类。</summary>
public class SmartFactoryException(string message, Exception? inner = null) : Exception(message, inner);
/// <summary>业务规则不满足时抛出。</summary>
public sealed class BusinessException(string message) : SmartFactoryException(message);
/// <summary>账号认证失败时抛出。</summary>
public sealed class AuthenticationException(string message) : SmartFactoryException(message);
/// <summary>当前角色不具备所需权限时抛出。</summary>
public sealed class AuthorizationException(string message) : SmartFactoryException(message);
/// <summary>远程接口调用失败时使用。</summary>
public sealed class NetworkException(string message, Exception? inner = null) : SmartFactoryException(message, inner);
/// <summary>设备通信或实时数据源异常。</summary>
public sealed class DeviceCommunicationException(string message, Exception? inner = null) : SmartFactoryException(message, inner);
