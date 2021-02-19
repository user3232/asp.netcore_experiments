# must use :5000 because it is http access point
# :5001 is for https requests
curl --location --request POST 'localhost:5001/api/questions' \
--header 'Content-Type: application/json' \
-D - \
--data-raw '{
    "title": "Sending request form Postman",
    "content": "Does not matter",
    "userId": "1",
    "userName": "bob.test@test.com",
    "created":"2020-09-20T14:26:45"
}' \
-E ../Certs/my_dev_cert.pfx

# # generated from vscode plugin (rest ...)
# curl --request POST \
#   --url http://localhost:5000/api/questions \
#   --header 'content-type: application/json' \
#   --header 'host: localhost:5000' \
#   --header 'user-agent: vscode-restclient' \
#   --data '{"title": "Sending request form Postman","content": "Does not matter","userId": "1","userName": "bob.test@test.com","created":"2020-09-20T14:26:45"}'