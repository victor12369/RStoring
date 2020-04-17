# RStoring

A general version-compatible serialization library for .Net.

Based on `System.Runtime.Serialization`.

In order to solve version-compatible and other Issues of `System.Runtime.Serialization`.

<br/>

---

<br/>

RStoring是一个基于 `System.Runtime.Serialization` 的序列化库，解决了 
`System.Runtime.Serialization` 的版本兼容之类的问题。

### System.Runtime.Serialization 的问题
* 改变类名、命名空间、字段名反序列化出错。
* `BinaryFormatter` 为二进制格式，为手动恢复数据带来困难。
* 更新版本时添加新字段无法兼容。

### 解决方法&特性
* 对每个类和要序列化的字段手动设置唯一ID，不因名称和命名空间的改变而改变。
* 提供 `RStoring.DeserializeBinaryFormatToDictionary()` 等方法，可反序列化为 `Dictionary<string,object>` 和 JSON 字符串。
* 通过设置默认值或默认初始化函数对不在序列化信息内的字段赋值，解决版本兼容问题。


### 使用方法
见项目 `Test`

### 注意
.Net 的反序列化机制存在安全问题，所以此项目亦然。
请使用受信任的序列化信息来源。

详见：[.Net反序列化漏洞](https://github.com/Ivan1ee/NET-Deserialize)

<br/>

---

<br/>

RStoring is a serialization library based on `System.Runtime.Serialization`, which solves
Issues such as `System.Runtime.Serialization` version compatibility.

### Problems with System.Runtime.Serialization
* Deserialization error when changing class name, namespace and field name.
* `BinaryFormatter` is a binary format, which makes it difficult to recover data manually.
* Adding new fields when updating the version is not compatible.

### Solutions & Features
* Manually set a unique ID for each class and field to be serialized, and it will not be changed due to the change of name and namespace.
* Provide `RStoring.DeserializeBinaryFormatToDictionary()` and other methods, which can be deserialized into `Dictionary<string,object>` and JSON string.
* Solve version compatibility problems by setting default values ​​or default initialization functions to assign values ​​to fields that are not in serialized information.


### Instructions
See project `Test`

### NOTE
.Net's deserialization mechanism has security issues, so this project is no different.
Please use a trusted source of serialized information.

For details, see: [.Net Deserialization Vulnerability](https://github.com/Ivan1ee/NET-Deserialize)