// Get access token if required
string token = this.ThisLib.getAccessToken();

try
{
    // Initialize the HttpClient
    var client = new System.Net.Http.HttpClient();
    
    // Get API URL from UDCodes table (optional, can be hardcoded)
    var tblRfxcelEvent = (from row in Db.UDCodes 
                          where row.CodeTypeID == "RTSCRED" && row.CodeID == "EVENT_API" 
                          select row).FirstOrDefault();
    
    string eventApiUrl = tblRfxcelEvent?.LongDesc ?? "URL WILL COME HERE";

    // Define business step variable
    string bizStepReceiving = "urn:epcglobal:cbv:bizstep:receiving";

    // Create HTTP request
    var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, eventApiUrl);

    // Set request headers
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    client.DefaultRequestHeaders.Add("Cookie", "JSESSIONID=A14EB44A64F7FCCFDA3C59E75E65E7BE");

    // Set JSON request body with variable
    string jsonContent = $@"{{
        ""epcisBody"": {{
            ""eventList"": [
                {{
                    ""objectEvent"": {{
                        ""eventId"": ""REG-TEST:MFR:LOT1:PALLET:RECEIVE:1"",
                        ""eventTime"": ""2025-01-27T08:42:28.003Z"",
                        ""eventTimeZoneOffset"": ""+00:00"",
                        ""epcList"": [
                            ""urn:epc:id:sgtin:0370756.070130.33786974340213""
                        ],
                        ""action"": ""OBSERVE"",
                        ""bizStep"": ""{bizStepReceiving}"",
                        ""disposition"": ""urn:epcglobal:cbv:disp:in_progress"",
                        ""readPoint"": {{
                            ""id"": ""urn:epc:id:sgln:0897826002.00.0""
                        }},
                        ""bizLocation"": {{
                            ""id"": ""urn:epc:id:sgln:0897826002.00.0""
                        }},
                        ""bizTransactionList"": [
                            {{
                                ""type"": ""urn:epcglobal:cbv:btt:po"",
                                ""bizTransaction"": ""411600001""
                            }}
                        ],
                        ""extension"": {{
                            ""sourceList"": [
                                {{
                                    ""type"": ""location"",
                                    ""source"": ""urn:epc:id:sgln:08656240003.8.0""
                                }},
                                {{
                                    ""type"": ""possessing_party"",
                                    ""source"": ""urn:epc:id:sgln:08656240003.8.0""
                                }},
                                {{
                                    ""type"": ""owning_party"",
                                    ""source"": ""urn:epc:id:sgln:0370756.00000.0 ""
                                }}
                            ],
                            ""destinationList"": [
                                {{
                                    ""type"": ""location"",
                                    ""destination"": ""urn:epc:id:sgln:0897826002.00.0""
                                }},
                                {{
                                    ""type"": ""possessing_party"",
                                    ""destination"": ""urn:epc:id:sgln:0897826002.00.0""
                                }},
                                {{
                                    ""type"": ""owning_party"",
                                    ""destination"": ""urn:epc:id:sgln:0897826002.00.0""
                                }}
                            ]
                        }}
                    }}
                }}
            ]
        }}
    }}";

    // Set request content
    var content = new System.Net.Http.StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
    request.Content = content;

    // Send request and get response synchronously
    var response = client.SendAsync(request).GetAwaiter().GetResult();
    
    // Ensure success status
    response.EnsureSuccessStatusCode();

    // Read response content
    string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

    // Log response in Epicor
    this.PublishInfoMessage($"API Response: {responseBody}", Ice.Common.BusinessObjectMessageType.Information, Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
}
catch (Exception ex)
{
    // Log error message
    this.PublishInfoMessage($"Error: {ex.Message}\nInner Exception: {ex.InnerException?.Message}", Ice.Common.BusinessObjectMessageType.Error, Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
}
