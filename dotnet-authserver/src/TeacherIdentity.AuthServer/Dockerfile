# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:6.0
ARG GIT_SHA

RUN apt-get update \
  && apt-get install --yes --no-install-recommends openssh-server \
  # remove below line after microsoft updates debian to 11 in aspnet:6.0 base image
  && apt-get install --yes --no-install-recommends libc6=2.31-13+deb11u4 libc-bin=2.31-13+deb11u4 libpcre2-8-0=10.36-2+deb11u1 \
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

COPY bin/Release/net6.0/publish/ App/

ENV GitSha ${GIT_SHA}
WORKDIR /App
ENTRYPOINT ["/bin/sh", "-c", "/usr/sbin/sshd && dotnet /App/TeacherIdentity.AuthServer.dll"]

EXPOSE 80 2222
