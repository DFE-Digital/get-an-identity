# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:7.0
ARG GIT_SHA

RUN apt-get update \
  && apt-get install --yes --no-install-recommends openssh-server \
  && echo "root:Docker!" | chpasswd

COPY <<EOF /etc/ssh/sshd_config
Port 			2222
ListenAddress 		0.0.0.0
LoginGraceTime 		180
X11Forwarding 		yes
Ciphers aes128-cbc,3des-cbc,aes256-cbc,aes128-ctr,aes192-ctr,aes256-ctr
MACs hmac-sha1,hmac-sha1-96
StrictModes 		yes
SyslogFacility 		DAEMON
PasswordAuthentication 	yes
PermitEmptyPasswords 	no
PermitRootLogin 	yes
Subsystem sftp internal-sftp
EOF

RUN ssh-keygen -A && mkdir -p /var/run/sshd

COPY bin/Release/net7.0/publish/ App/

ENV GitSha ${GIT_SHA}
WORKDIR /App
ENTRYPOINT ["/bin/sh", "-c", "/usr/sbin/sshd && dotnet /App/TeacherIdentity.AuthServer.dll"]

EXPOSE 80 2222
