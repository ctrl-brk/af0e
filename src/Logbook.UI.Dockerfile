FROM node

WORKDIR /app
COPY AF0E.UI/Site/package.json .
RUN npm i
COPY AF0E.UI/Site/. .

EXPOSE 4200

CMD ["npm", "start"]
